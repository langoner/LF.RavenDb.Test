using System;
using System.Collections.Generic;
using System.Linq;
using LF.RavenDb.Test.ScriptedPatchPerformance.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;

namespace LF.RavenDb.Test.ScriptedPatchPerformance
{
    public class ScopeIndex : AbstractMultiMapIndexCreationTask<SocialMask>
    {

        public ScopeIndex()
        {
            AddMap<SocialMask>(masks => from mask in masks
                                        select new
                                        {
                                            Spheres = mask.Spheres.Select(x => x.Id)
                                        });
        }
    }

    [TestClass]
    public class ScriptedPatchPerformanceTest
    {

        public sealed class TestStore : DocumentStore
        {
            public TestStore()
            {
                Url = "http://localhost:8080";
                DefaultDatabase = "LFDbTest";
                Conventions.IdentityPartsSeparator = "-";
                Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
                Initialize();
            }
        }

        private static IDocumentStore _store;
        private const int LincCount = 13;
        private IDocumentSession _session;
        private DateTime _start, _end;

        #region Init func
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _store = new TestStore();
            IndexCreation.CreateIndexes(typeof(ScopeIndex).Assembly, _store);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _store.Dispose();
            _store = null;
        }

        [TestInitialize]
        public void Init()
        {
            using (var session = _store.OpenSession())
            {
                session.Store(new ContactSphere { Id = "ContactSpheres-1", Acl = new[] { "user-1", "user-2", "user-3" } });

                for (int i = 1; i <= LincCount; i++)
                {
                    session.Store(new SocialMask
                    {
                        Id = string.Format("SocialMask-{0}", i),
                        Spheres = new[] { new ContactSphere.Link { Id = "ContactSpheres-1" } }
                    });
                }
                session.SaveChanges();
            }

            _session = _store.OpenSession();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _session.Dispose();
            _session = null;

            using (var session = _store.OpenSession())
            {
                session.Delete(session.Load<ContactSphere>("ContactSpheres-1"));
                for (int i = 1; i <= LincCount; i++)
                {
                    session.Delete(session.Load<SocialMask>(string.Format("SocialMask-{0}", i)));
                }
                session.SaveChanges();
            }
        }
        #endregion 

        [TestMethod]
        public void ScriptedByKeyNotFoundAcl()
        {
            _start = DateTime.Now;
            _session.Advanced.DocumentStore.DatabaseCommands.Patch(
                "ContactSpheres-1",
                new ScriptedPatchRequest
                    {
                        Script = @"this.Acl.RemoveWhere(function(item) { return item == friend; });",
                        Values = new Dictionary<string, object> { { "friend", "user" } }
                    }
                );
            _session.SaveChanges();
            _end = DateTime.Now;
            var time = _end - _start;
            Assert.IsTrue(time.TotalMilliseconds < 500, "Too long. {0} ms.", time.TotalMilliseconds);
        }

        [TestMethod]
        public void ScriptedByKeyFoundAcl()
        {
            _start = DateTime.Now;
            _session.Advanced.DocumentStore.DatabaseCommands.Patch(
                "ContactSpheres-1",
                new ScriptedPatchRequest
                {
                    Script = @"this.Acl.RemoveWhere(function(item) { return item == friend; });",
                    Values = new Dictionary<string, object> { { "friend", "user-1" } }
                }
                );
            _session.SaveChanges();
            _end = DateTime.Now;
            var time = _end - _start;
            Assert.IsTrue(time.TotalMilliseconds < 500, "Too long. {0} ms.", time.TotalMilliseconds);
        }

        [TestMethod]
        public void ScriptedByIndexNotFoundEntity()
        {
            _start = DateTime.Now;
            _session.Advanced.DocumentStore.DatabaseCommands.UpdateByIndex(
                "ScopeIndex",
                new IndexQuery { Query = string.Format("Spheres:ContactSpheres-1") },
                new ScriptedPatchRequest
                {
                    Script = @"
                                this.Spheres.Map(function(item) {
                                    if(item.Id == sphereId) {
                                        item.Acl.push(friend);
                                    return item;
                                    }
                                });
                            ",
                    Values = new Dictionary<string, object>
                                {
                                    { "sphereId", "NotExistSphere" }, 
                                    { "friend", "user-1" }
                                }
                });
            _session.SaveChanges();
            _end = DateTime.Now;
            var time = _end - _start;
            Assert.IsTrue(time.TotalMilliseconds < 500, "Too long. {0} ms.", time.TotalMilliseconds);
        }

        [TestMethod]
        public void ScriptedByIndexFoundEntity()
        {
            _start = DateTime.Now;
            _session.Advanced.DocumentStore.DatabaseCommands.UpdateByIndex(
                "ScopeIndex",
                new IndexQuery { Query = string.Format("Spheres:ContactSpheres-1") },
                new ScriptedPatchRequest
                {
                    Script = @"
                                this.Spheres.Map(function(item) {
                                    if(item.Id == sphereId) {
                                        item.Acl.push(friend);
                                    return item;
                                    }
                                });
                            ",
                    Values = new Dictionary<string, object>
                                {
                                    { "sphereId", "ContactSpheres-1" }, 
                                    { "friend", "user-1" }
                                }
                });

            
            _session.SaveChanges();
            _end = DateTime.Now;
            var time = _end - _start;
            Assert.IsTrue(time.TotalMilliseconds < 500, "Too long. {0} ms.", time.TotalMilliseconds);
        }
    }
}
