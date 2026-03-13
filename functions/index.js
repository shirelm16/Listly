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
