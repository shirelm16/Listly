using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class CategoryOverrideDocument
    {
        [FirestoreProperty("category")]
        public string Category { get; set; }

        [FirestoreProperty("LastModified")]
        public long LastModifiedUnix { get; set; }
    }
}
