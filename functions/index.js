const {onDocumentWritten} = require("firebase-functions/v2/firestore");
const {onRequest} = require("firebase-functions/v2/https");
const {defineSecret} = require("firebase-functions/params");
const admin = require("firebase-admin");
const OpenAI = require("openai");

admin.initializeApp();

const openAiApiKey = defineSecret("OPENAI_API_KEY");

let openAiClient = null;
/**
 * Returns a singleton OpenAI client instance.
 * @return {OpenAI} The OpenAI client.
 */
function getOpenAiClient() {
  if (!openAiClient) {
    openAiClient = new OpenAI({apiKey: openAiApiKey.value()});
  }
  return openAiClient;
}

exports.notifyOnSharedListChange = onDocumentWritten(
    "shoppingLists/{listId}/items/{itemId}",
    async (event) => {
      const listId = event.params.listId;

      console.log("Triggered function for listId:", listId);

      const change = {
        before: event.data && event.data.before,
        after: event.data && event.data.after,
      };

      let action = "";
      let itemName = "";

      if (!change.before.exists && change.after.exists) {
        action = "added";
        itemName = change.after.data().name;
      } else if (change.before.exists && !change.after.exists) {
        action = "deleted";
        itemName = change.before.data().name;
      } else if (change.before.exists && change.after.exists) {
        itemName = change.after.data().name;
        if (change.before.data().isPurchased !==
          change.after.data().isPurchased) {
          action = change.after.data().isPurchased ?
          "purchased" : "unpurchased";
        } else {
          action = "edited";
        }
      }

      if (!action || !itemName) return;

      const listSnap = await admin
          .firestore()
          .doc(`shoppingLists/${listId}`)
          .get();

      const listData = listSnap.data();
      if (!listData || !listData.collaborators) return;
      const {collaborators, ownerId, lastModifiedUser} = listData;

      const recipientIds = new Set(collaborators);
      recipientIds.add(ownerId);

      if (lastModifiedUser) {
        recipientIds.delete(lastModifiedUser);
      }

      const tokens = [];
      for (const userId of recipientIds) {
        const userDoc = await admin.firestore().doc(`users/${userId}`).get();
        if (userDoc.exists) {
          const userData = userDoc.data();
          if (userData.deviceToken) {
            tokens.push(userData.deviceToken);
          }
        }
      }

      console.log("Tokens to send:", tokens);

      if (tokens.length > 0) {
        await admin.messaging().sendEachForMulticast({
          tokens,
          data: {
            title: `List "${listData.name}" updated`,
            body: `Item "${itemName}" was ${action}`,
          },
          android: {priority: "high"},
        });
      }
    });

exports.categorizeItem = onRequest(
    {
      secrets: [openAiApiKey],
      minInstances: 1,
      timeoutSeconds: 10,
      region: "me-west1",
    },
    async (req, res) => {
      try {
        if (req.method !== "POST") {
          res.status(405).send("Method not allowed");
          return;
        }

        const itemName = req.body && req.body.itemName;
        const userId = req.body && req.body.userId;

        if (!itemName || typeof itemName !== "string") {
          res.status(400).send("Missing itemName");
          return;
        }

        if (!userId || typeof userId !== "string") {
          res.status(400).send("Missing userId");
          return;
        }

        const normalizedItemName = itemName
            .trim()
            .toLowerCase()
            .replace(/\s+/g, " ");

        const userOverrideRef = admin
            .firestore()
            .collection("users")
            .doc(userId)
            .collection("categoryOverrides")
            .doc(normalizedItemName);

        const cacheRef = admin
            .firestore()
            .collection("categoryCache")
            .doc(normalizedItemName);

        const [userOverrideDoc, cacheDoc] = await Promise.all([
          userOverrideRef.get(),
          cacheRef.get(),
        ]);

        if (userOverrideDoc.exists) {
          res.json({
            category: userOverrideDoc.data().category,
            source: "user_override",
          });
          return;
        }

        if (cacheDoc.exists) {
          res.json({
            category: cacheDoc.data().category,
            source: "cache",
          });
          return;
        }

        const client = getOpenAiClient();

        const categories = req.body && req.body.categories;

        if (!Array.isArray(categories) || categories.length === 0) {
          res.status(400).send("Missing categories");
          return;
        }

        const messages = [
          {
            role: "system",
            content: `
    You classify grocery shopping items into categories.
    You must return exactly one category from the provided list.
    If the item does not clearly belong to any category, return "Other".
    Do not explain your answer.
    `,
          },
          {
            role: "user",
            content: `
    Item name: "${itemName}"

    Categories:
    ${categories.join(", ")}

    Return only the category name.
    `,
          }];
        console.log("Calling OpenAI for:", itemName);
        const response = await client.chat.completions.create({
          model: "gpt-4o-mini",
          messages: messages,
          temperature: 0,
        });
        console.log("OpenAI response:", response);
        const category = response.choices[0].message.content.trim();

        await cacheRef.set({
          category,
          createdAt: admin.firestore.FieldValue.serverTimestamp(),
        });

        res.json({
          category,
          source: "ai",
        });
      } catch (error) {
        console.error("categorizeItem error:", error);
        res.status(500).send("Internal server error");
      }
    },
);

exports.extractRecipeItems = onRequest(
    {
      secrets: [openAiApiKey],
      minInstances: 1,
      region: "me-west1",
    },
    async (req, res) => {
      try {
        if (req.method !== "POST") {
          res.status(405).send("Method not allowed");
          return;
        }

        const url = req.body.url;
        if (!url || typeof url !== "string") {
          res.status(400).send("Missing url");
          return;
        }

        const categories = req.body && req.body.categories;

        if (!Array.isArray(categories) || categories.length === 0) {
          res.status(400).send("Missing categories");
          return;
        }

        // Fetch the page
        const pageResponse = await fetch(url);
        if (!pageResponse.ok) {
          res.status(400).send("Failed to fetch URL");
          return;
        }

        // Strip HTML tags and truncate to keep tokens low
        const html = await pageResponse.text();
        const plainText = html
            .replace(/<script[\s\S]*?<\/script>/gi, "")
            .replace(/<style[\s\S]*?<\/style>/gi, "")
            .replace(/<[^>]*>/g, " ")
            .replace(/\s+/g, " ")
            .trim()
            .slice(0, 8000);

        const client = getOpenAiClient();

        const response = await client.chat.completions.create({
          model: "gpt-4o",
          temperature: 0,
          messages: [
            {
              role: "system",
              content: `You extract shopping ingredients from recipe text.
  Return a JSON array only. No explanation, no markdown, no code blocks.
  Each element must have exactly these fields:
  { "name": string, 
    "quantity": number | null, 
    "unit": string | null, 
    "category": string }

  Available categories:
  ${categories.join(", ")}
  
  Rules:
  - If quantity is unspecified, vague (e.g. "to taste", "as needed", "לפי הטעם")
    set BOTH quantity and unit to null
  - For small measurements (teaspoons, tablespoons, כף, כפית and similar) 
    set BOTH quantity and unit to null
  - If quantity is null, unit must also be null
  - "cup"/"כוס" is acceptable as a unit, 
    optionally convert to ml or grams if straightforward
  - If the recipe mentions alternatives 
    e.g. "zucchini or eggplant" / "דלורית/דלעת/גזר"), 
    combine them into a single item 
    with a slash separator: "דלורית / דלעת / גזר".
    Do not create separate items for alternatives
  - If alternatives include a generic non-ingredient word 
    like "אחר" (other) or "אחרת", 
    remove it. Example: "שמן זית / אחר" → "שמן זית". 
    If only one ingredient remains, write it alone without a slash.
  - Do not include water or boiling water ("מים", "מים רותחים") 
    as a shopping item
  - Countable items (pieces of meat, whole vegetables, eggs, etc.) 
    should keep their quantity as a number with unit null
  - If a quantity is given as a range (e.g. "6-8", "2-3"), use the lower number
  - For poultry and meat described as "מנות" or "חלקים", 
    treat the number as the quantity with unit null.
  - Do not include optional garnishes or "for serving" items
  - Assign each item exactly one category from the provided list. 
    If none fit, use "Other"`,
            },
            {
              role: "user",
              content: plainText,
            },
          ],
        });

        const raw = response.choices[0].message.content.trim();

        // Strip markdown code blocks if present
        const cleaned = raw
            .replace(/^```json\s*/i, "")
            .replace(/^```\s*/i, "")
            .replace(/```$/i, "")
            .trim();

        // temporary, remove after debugging
        console.log("OpenAI raw response:", raw);

        const items = JSON.parse(cleaned);

        res.json({items});
      } catch (error) {
        console.error("extractRecipeItems error:", error);
        res.status(500).send("Internal server error");
      }
    },
);
