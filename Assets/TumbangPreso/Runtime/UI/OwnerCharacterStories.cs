using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    [Serializable]
    public sealed class OwnerCharacterStory
    {
        public string id,origin,homeCourt,shortLine,introduction;
    }
    public static class OwnerCharacterStories
    {
        [Serializable]private sealed class Book { public OwnerCharacterStory[] stories; }
        private static Book _book;
        public static OwnerCharacterStory For(string id)
        {
            if(_book==null)
            {
                var asset=Resources.Load<TextAsset>("UI/character-stories");
                _book=asset!=null?JsonUtility.FromJson<Book>(asset.text):new Book();
            }
            if(_book.stories!=null)foreach(var story in _book.stories)if(story!=null && story.id==id)return story;
            return null;
        }
    }
}
