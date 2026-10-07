using System;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SceneryTextureIdentityTests
    {
        private static Texture2D Texture(Color colour)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Same texture name"};
            texture.SetPixels(new[]{colour,colour,colour,colour});texture.Apply();return texture;
        }
        [Test]public void PreparedLookupUsesExactReferenceEvenWhenNameAndDimensionsMatch()
        {
            var red=Texture(Color.red);var blue=Texture(Color.blue);var book=ScriptableObject.CreateInstance<SceneryTextureIdentities>();
            try
            {
                string a=SceneryTextureIdentities.Resolve(red),b=SceneryTextureIdentities.Resolve(blue);
                Assert.AreNotEqual(a,b,"Same-size pixel changes must change provenance.");
                book.Entries=new[]{new SceneryTextureIdentities.Entry{Texture=red,Hash=a},new SceneryTextureIdentities.Entry{Texture=blue,Hash=b}};
                red.Apply(false,true);blue.Apply(false,true);
                Assert.IsFalse(red.isReadable);Assert.IsFalse(blue.isReadable);
                Assert.AreEqual(a,SceneryTextureIdentities.ResolvePrepared(red,book));
                Assert.AreEqual(b,SceneryTextureIdentities.ResolvePrepared(blue,book));
            }
            finally{Object.DestroyImmediate(book);Object.DestroyImmediate(red);Object.DestroyImmediate(blue);}
        }
        [Test]public void ReadableDynamicPixelMutationChangesIdentity()
        {
            var texture=Texture(Color.red);
            try
            {
                string before=SceneryTextureIdentities.Resolve(texture);
                texture.SetPixel(0,0,Color.blue);texture.Apply();
                Assert.AreNotEqual(before,SceneryTextureIdentities.Resolve(texture));
            }
            finally{Object.DestroyImmediate(texture);}
        }
        [Test]public void UnpreparedNonReadableTextureDoesNotDegradeToNameOrDimensions()
        {
            var texture=Texture(Color.green);var book=ScriptableObject.CreateInstance<SceneryTextureIdentities>();
            try{texture.Apply(false,true);Assert.Throws<InvalidOperationException>(()=>SceneryTextureIdentities.ResolvePrepared(texture,book));}
            finally{Object.DestroyImmediate(book);Object.DestroyImmediate(texture);}
        }
        [Test]public void NullTextureHasTheExistingEmptyIdentity()
        {Assert.AreEqual("",SceneryTextureIdentities.Resolve(null));Assert.AreEqual("",SceneryTextureIdentities.ResolvePrepared(null,null));}
    }
}
