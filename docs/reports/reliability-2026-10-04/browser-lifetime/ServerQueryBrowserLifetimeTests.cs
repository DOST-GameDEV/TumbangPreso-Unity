using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ServerQueryBrowserLifetimeTests
    {
        private GameObject _root;
        private ServerQuery _browser;
        private int _notifications;
        [SetUp] public void Before()
        {
            _notifications=0;_root=new GameObject("Dormant browser lifetime query");_root.SetActive(false);
            _browser=_root.AddComponent<ServerQuery>();_browser.ServersChanged+=()=>_notifications++;
        }
        [TearDown] public void After(){if(_root!=null)Object.DestroyImmediate(_root);}
        private Task Refresh(Func<Task<bool>> auth,Func<QueryLobbiesOptions,Task<QueryResponse>> query)
            => (Task)typeof(ServerQuery).GetMethod("RefreshOnlineLobbiesWithDispatchAsync",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(_browser,new object[]{auth,query});
        private static QueryResponse Response(string id)=>new QueryResponse(new List<Lobby>{new Lobby(id:id,name:id,maxPlayers:4)});
        private static Task<bool> Auth()=>Task.FromResult(true);

        [Test] public async Task ReplyAfterCloseDoesNotRepopulateRooms()
        {
            _browser.StartBrowsing();var reply=new TaskCompletionSource<QueryResponse>();
            var pending=Refresh(Auth,_=>reply.Task);_browser.StopBrowsing();reply.SetResult(Response("closed-room"));await pending;
            Assert.IsEmpty(_browser.Servers,"A closed browser accepted its late lookup.");Assert.AreEqual(0,_notifications);
        }
        [Test] public async Task ReplyFromPreviousOpeningCannotPopulateReopenedBrowser()
        {
            _browser.StartBrowsing();var reply=new TaskCompletionSource<QueryResponse>();
            var pending=Refresh(Auth,_=>reply.Task);_browser.StopBrowsing();_browser.StartBrowsing();reply.SetResult(Response("old-opening"));await pending;
            Assert.IsEmpty(_browser.Servers,"A reopened browser accepted the previous opening's lookup.");
            await Refresh(Auth,_=>Task.FromResult(Response("current-opening")));
            Assert.AreEqual("current-opening",_browser.Servers.Single().Id);Assert.AreEqual(1,_notifications);
        }
        [Test] public async Task AuthenticationFinishingAfterCloseDoesNotDispatchLookup()
        {
            _browser.StartBrowsing();var auth=new TaskCompletionSource<bool>();int queries=0;
            var pending=Refresh(()=>auth.Task,_=>{queries++;return Task.FromResult(Response("closed-room"));});
            _browser.StopBrowsing();auth.SetResult(true);await pending;
            Assert.AreEqual(0,queries,"Closed browsing still dispatched a service lookup.");Assert.IsEmpty(_browser.Servers);
        }
        [Test] public async Task CurrentOpeningStillPublishesItsRoom()
        {
            _browser.StartBrowsing();await Refresh(Auth,_=>Task.FromResult(Response("current-room")));
            Assert.AreEqual("current-room",_browser.Servers.Single().Id);Assert.AreEqual(1,_notifications);
        }
        [Test] public async Task ExplicitRefreshWithoutAutomaticBrowsingRemainsSupported()
        {
            await Refresh(Auth,_=>Task.FromResult(Response("manual-room")));
            Assert.AreEqual("manual-room",_browser.Servers.Single().Id);Assert.AreEqual(1,_notifications);
        }
    }
}
