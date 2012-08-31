using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LF.RavenDb.Test.ScriptedPatchPerformance.Model
{
    [Serializable]
    [DataContract]
    public abstract class EntityBase
    {
        /// <summary>
        /// Ключ сущности
        /// </summary>
        [DataMember]
        public string Id { get; set; }
    }

    [DataContract]
    public class ContactSphere : EntityBase
    {
        /// <summary>
        /// Denormalised link class
        /// </summary>
        public class Link
        {
            public Link()
            {
                Acl = new string[0];
            }

            public string Id { get; set; }
            public string[] Acl { get; set; }
        }

        public ContactSphere()
        {
            Acl = new string[0];
        }
        
        [DataMember]
        public string[] Acl { get; set; }
    }


    [DataContract]
    public class SocialMask : EntityBase
    {
        public SocialMask()
        {
            Spheres = new ContactSphere.Link[0];
        }
        
        [DataMember]
        public ContactSphere.Link[] Spheres { get; set; }
    }
}
