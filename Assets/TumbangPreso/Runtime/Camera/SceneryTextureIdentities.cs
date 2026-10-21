using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Imported texture provenance is prepared by the build pipeline. References
    // identify the exact asset; names and dimensions are never a substitute.
    public sealed class SceneryTextureIdentities : ScriptableObject
    {
        public const string ResourcePath="SceneryTextureIdentities";
        [Serializable] public struct Entry { public Texture Texture; public string Hash; }
        public Entry[] Entries=Array.Empty<Entry>();
        public string WhiteHash,BlackHash,GrayHash,NormalHash;
        private static SceneryTextureIdentities _book;
        private static Dictionary<Texture,string> _hashes;

        public static string Resolve(Texture texture)
        {
            if(texture==null)return "";
#if UNITY_EDITOR
            if(UnityEditor.EditorUtility.IsPersistent(texture))return texture.imageContentsHash.ToString();
#endif
            EnsureBook();
            var builtin=BuiltinHash(texture,_book);
            if(builtin!=null)return builtin;
            if(_hashes.TryGetValue(texture,out var imported))return imported;
            if(texture is Texture2D readable && readable.isReadable)
            {
                using var sha=SHA256.Create();
                return "pixels:"+BitConverter.ToString(sha.ComputeHash(readable.GetRawTextureData())).Replace("-","");
            }
#if UNITY_EDITOR
            return texture.imageContentsHash.ToString();
#else
            throw new InvalidOperationException("Scenery texture identity was not prepared: "+texture.name);
#endif
        }

        // This path is also exercised in Editor tests without invoking the
        // Editor-only property, matching the actual player lookup.
        public static string ResolvePrepared(Texture texture,SceneryTextureIdentities book)
        {
            if(texture==null)return "";
            if(book==null)throw new InvalidOperationException("Scenery texture identity table is missing.");
            var builtin=BuiltinHash(texture,book);if(builtin!=null)return builtin;
            foreach(var entry in book.Entries)
                if(entry.Texture==texture && !string.IsNullOrEmpty(entry.Hash))return entry.Hash;
            throw new InvalidOperationException("Scenery texture identity was not prepared: "+texture.name);
        }
        private static string BuiltinHash(Texture texture,SceneryTextureIdentities book)
        {
            string value=null;bool builtin=true;
            if(texture==Texture2D.whiteTexture)value=book?.WhiteHash;
            else if(texture==Texture2D.blackTexture)value=book?.BlackHash;
            else if(texture==Texture2D.grayTexture)value=book?.GrayHash;
            else if(texture==Texture2D.normalTexture)value=book?.NormalHash;
            else builtin=false;
            if(builtin && string.IsNullOrEmpty(value))throw new InvalidOperationException("Builtin scenery texture identity was not prepared: "+texture.name);
            return builtin?value:null;
        }
        private static void EnsureBook()
        {
            if(_hashes!=null)return;
            var book=Resources.Load<SceneryTextureIdentities>(ResourcePath);
            var hashes=new Dictionary<Texture,string>();
            if(book==null){_hashes=hashes;return;}
            foreach(var entry in book.Entries)
            {
                if(entry.Texture==null || string.IsNullOrEmpty(entry.Hash))throw new InvalidOperationException("Invalid scenery texture identity table.");
                if(hashes.ContainsKey(entry.Texture))throw new InvalidOperationException("Duplicate scenery texture identity.");
                hashes.Add(entry.Texture,entry.Hash);
            }
            // Failed validation must never publish a partially filled cache.
            _book=book;_hashes=hashes;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){_book=null;_hashes=null;}
    }
}
