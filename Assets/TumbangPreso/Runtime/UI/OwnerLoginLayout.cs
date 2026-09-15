using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerLoginLayout
    {
        [Serializable] public sealed class Piece
        {
            public string name;public float x,y,width,height,faceX,faceY;
            public Rect Rect=>new Rect(x,y,width,height);
            public Vector2 Size=>new Vector2(width,height);
            public Vector2 Face=>new Vector2(faceX,faceY);
        }
        [Serializable] private sealed class Book { public Piece[] pieces; }
        private static Book _book;
        public static Piece Get(string name)
        {
            if(_book==null)_book=JsonUtility.FromJson<Book>(Resources.Load<TextAsset>("UI/owner-menu-edits/login-layout-v2").text);
            foreach(var piece in _book.pieces)if(piece.name==name)return piece;
            throw new InvalidOperationException("Missing supplied login layout: "+name);
        }
        public static void Place(Transform target,string name)
        {
            var p=Get(name);OwnerUiLayout.Place((RectTransform)target,p.x,p.y,p.width,p.height);
        }
    }
}
