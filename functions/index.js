const {onDocumentWritten} = require("firebase-functions/v2/firestore");
const admin = require("firebase-admin");

admin.initializeApp();

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
          if (userData.deviceTokens) {
            tokens.push(...userData.deviceTokens);
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
