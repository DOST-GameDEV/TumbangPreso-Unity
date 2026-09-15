using UnityEngine;

namespace TumbangPreso.UI
{
    public static class OwnerUiEntry
    {
        public static UnityEngine.UI.InputField Create(Transform parent,string name,string placeholder,
            OwnerUiTheme.Piece frame,OwnerUiGlyph.Mark? glyph=null,bool password=false,Sprite artwork=null,bool embeddedGlyph=false)
        {
            var art=OwnerUiLayout.Art(parent,name,frame);art.raycastTarget=true;
            if(artwork!=null)art.sprite=artwork;
            var input=art.gameObject.AddComponent<UnityEngine.UI.InputField>();
            input.targetGraphic=art;input.transition=UnityEngine.UI.Selectable.Transition.None;
            input.lineType=UnityEngine.UI.InputField.LineType.SingleLine;
            input.contentType=password?UnityEngine.UI.InputField.ContentType.Password:UnityEngine.UI.InputField.ContentType.Standard;
            input.characterLimit=password?128:254;input.caretColor=OwnerUiTheme.Current.ActionInk;
            input.customCaretColor=true;input.selectionColor=new Color(.73f,.81f,.27f,.45f);
            var area=OwnerUiLayout.Rect(art.transform,"EditableArea");OwnerUiLayout.Place(area,72,10,441,58);
            var text=OwnerUiLayout.Text(area,"EnteredText","",32);
            OwnerUiLayout.Fill(text.rectTransform);text.color=OwnerUiTheme.Current.EnteredInk;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;input.textComponent=text;
            var hint=OwnerUiLayout.Text(area,"Placeholder",placeholder,28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(hint.rectTransform);hint.color=OwnerUiTheme.Current.Ochre;
            input.placeholder=hint;
            if(embeddedGlyph)return input;
            if(glyph.HasValue)
            {
                var mark=OwnerUiGlyph.Create(art.transform,"FieldIcon",glyph.Value,OwnerUiTheme.Current.Ochre);
                OwnerUiLayout.Place(mark.rectTransform,25,23,28,29);
            }
            else
            {
                var mark=OwnerUiLayout.Art(art.transform,"PersonIcon",OwnerUiTheme.Piece.Person);
                OwnerUiLayout.Place(mark.rectTransform,25,23,28,29);
            }
            return input;
        }
    }
}
