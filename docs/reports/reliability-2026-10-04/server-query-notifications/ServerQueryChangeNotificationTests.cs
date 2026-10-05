using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ServerQueryChangeNotificationTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private ServerQuery _query;
        private ServerQuery.Entry _room;
        private int _notifications;

        [SetUp] public void Before()
        {
            _notifications = 0;
            _root = new GameObject("Dormant room notification query"); _root.SetActive(false);
            _query = _root.AddComponent<ServerQuery>();
            _room = new ServerQuery.Entry { Id = "synthetic-room", Name = "ROOM", Map = "Eskinita", Visibility = 0, Capacity = 4 };
            var rooms = (Dictionary<string, ServerQuery.Entry>)typeof(ServerQuery).GetField("_seen", Hidden).GetValue(_query);
            rooms.Add(_room.Id, _room);
            _query.ServersChanged += () => _notifications++;
            Notify(); Assert.AreEqual(1, _notifications); _notifications = 0;
        }
        [TearDown] public void After() { if (_root != null) Object.DestroyImmediate(_root); }
        private void Notify() => typeof(ServerQuery).GetMethod("RaiseIfChanged", Hidden).Invoke(_query, null);

        [Test] public void MapOnlyUpdateReachesSubscribers()
        {
            _room.Map = "Kanto"; Notify(); Assert.AreEqual(1, _notifications);
        }
        [Test] public void VisibilityOnlyUpdateReachesSubscribers()
        {
            _room.Visibility = 1; Notify(); Assert.AreEqual(1, _notifications);
        }
        [Test] public void RepeatedIdenticalRoomDoesNotRedraw()
        {
            Notify(); Notify(); Assert.AreEqual(0, _notifications);
        }
        [Test] public void ExistingCountUpdateStillReachesSubscribers()
        {
            _room.Occupied = 2; Notify(); Assert.AreEqual(1, _notifications);
        }
    }
}
