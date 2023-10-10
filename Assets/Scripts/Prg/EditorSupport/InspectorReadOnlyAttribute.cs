using UnityEngine;

namespace Prg.EditorSupport
{
    /// <summary>
    /// Attribute to decorate any value as read-only in Editor.
    /// </summary>
    /// <remarks>
    /// See: https://github.com/antonsem/extratools
    /// </remarks>
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
    }
}
