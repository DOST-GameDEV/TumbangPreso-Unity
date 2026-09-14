using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    [CreateAssetMenu(menuName="TUMP/UI/Owner painted theme",fileName="OwnerUiTheme")]
    public sealed class OwnerUiTheme : ScriptableObject
    {
        public enum Piece { Logo, AccountTabs, Person, FirstField, SecondField, ThirdField, Checkbox, LimeAction, LeftRule, RightRule, OrangeAction }
        public Color Ink=new Color32(147,17,32,255);
        public Color DeepInk=new Color32(120,13,29,255);
        public Color Paper=new Color32(223,212,215,255);
        public Color Pale=new Color32(240,233,235,255);
        public Color Peach=new Color32(224,181,144,255);
        public Color Lime=new Color32(187,208,69,255);
        public Color Orange=new Color32(241,175,78,255);
        public Color Ochre=new Color32(188,135,73,255);
        public Color Green=new Color32(15,89,19,255);
        public Color ActionInk=new Color32(144,18,25,255);
        public Color IdleTabInk=new Color32(161,46,52,255);
        public Color HintInk=new Color32(200,23,33,255);
        public Color EnteredInk=Color.black;
        public Color GuestInk=Color.white;
        public Font DisplayFont, AccentFont, ReadingFont;
        public Texture2D Artwork, Pattern;
        public float EnterSeconds=.28f, StateSeconds=.12f;
        public Vector2 ReferenceResolution=new Vector2(1920,1080);
        private readonly Dictionary<Piece,Sprite> _pieces=new Dictionary<Piece,Sprite>();
        private static OwnerUiTheme _current;
        public static OwnerUiTheme Current
        {
            get
            {
                if(_current!=null)return _current;
                _current=Resources.Load<OwnerUiTheme>("UI/owner-painted/OwnerUiTheme");
                if(_current==null){_current=CreateInstance<OwnerUiTheme>();_current.hideFlags=HideFlags.DontSave;}
                return _current;
            }
        }
        public Font Display=>DisplayFont!=null?DisplayFont:Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");
        public Font Accent=>AccentFont!=null?AccentFont:Resources.Load<Font>("UI/fonts/KawitExtended");
        public Font Reading=>ReadingFont!=null?ReadingFont:Resources.Load<Font>("UI/fonts/Lydian-Regular");
        public Texture2D Background=>Pattern!=null?Pattern:Resources.Load<Texture2D>("UI/owner-painted/background");
        public Sprite Art(Piece piece)
        {
            if(_pieces.TryGetValue(piece,out var sprite) && sprite!=null)return sprite;
            var atlas=Artwork!=null?Artwork:Resources.Load<Texture2D>("UI/owner-painted/artwork");
            if(atlas==null)return null;
            Rect top=SourceRect(piece);
            var rect=new Rect(top.x,atlas.height-top.y-top.height,top.width,top.height);
            sprite=Sprite.Create(atlas,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name="OwnerPainted_"+piece;sprite.hideFlags=HideFlags.DontSave;
            _pieces[piece]=sprite;return sprite;
        }
        // Measured source pixels, top-left convention; never resize the underlying artwork.
        public static Rect SourceRect(Piece piece)
        {
            switch(piece)
            {
                case Piece.Logo:return new Rect(760,44,407,273);
                case Piece.AccountTabs:return new Rect(770,337,383,88);
                case Piece.Person:return new Rect(622,475,28,29);
                case Piece.FirstField:return new Rect(695,452,533,78);
                case Piece.SecondField:return new Rect(695,545,533,77);
                case Piece.ThirdField:return new Rect(695,640,533,77);
                case Piece.Checkbox:return new Rect(712,725,21,24);
                case Piece.LimeAction:return new Rect(755,796,413,91);
                case Piece.LeftRule:return new Rect(705,907,242,6);
                case Piece.RightRule:return new Rect(1025,909,242,5);
                default:return new Rect(755,935,413,91);
            }
        }
    }
}
