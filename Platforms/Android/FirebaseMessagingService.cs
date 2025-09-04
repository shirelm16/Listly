using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Firebase.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Platforms.Android
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" }, Priority = 1000)]
    [Register("com.shirelm.listly.MyFirebaseMessagingService")]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            message.Data.TryGetValue("title", out var title);
            message.Data.TryGetValue("body", out var body);

            ShowNotification(title, body);
        }

        private void ShowNotification(string title, string body)
        {
            var channelId = "shopping_list_updates";

            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var notificationBuilder = new NotificationCompat.Builder(this, channelId)
                .SetSmallIcon(Microsoft.Maui.Resource.Drawable.ic_notification)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManagerCompat.From(this);

            // Android 8.0+ requires a channel
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Shopping List Updates", NotificationImportance.Default);
                notificationManager.CreateNotificationChannel(channel);
            }

            notificationManager.Notify(new Random().Next(), notificationBuilder.Build());
        }
    }
}
