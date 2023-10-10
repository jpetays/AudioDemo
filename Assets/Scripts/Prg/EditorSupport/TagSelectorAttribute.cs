// ------------------------------------------- //
// Author  : William Whitehouse / WSWhitehouse //
// GitHub  : github.com/WSWhitehouse           //
// Created : 30/06/2019                        //
// Edited  : 25/02/2020                        // 
// ------------------------------------------- //

using UnityEngine;

namespace Prg.EditorSupport
{
    public class TagSelectorAttribute : PropertyAttribute
    {
        // ReSharper disable once ConvertToConstant.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public bool UseEditorGui = false;
    }
}
