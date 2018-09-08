using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Trubkin.Util
{
    public static class EditorGui
    {
        private static bool expandList;

        public static void DrawList<T>(string label, IList<T> list, bool allowSceneObject = true) where T : Object
        {
            expandList = EditorGUILayout.Foldout(expandList, label);
            if (!expandList) return;

            EditorGUI.indentLevel++;
            var newCount = Mathf.Max(0, EditorGUILayout.IntField("Size", list.Count));
            while (newCount < list.Count)
                list.RemoveAt(list.Count - 1);
            while (newCount > list.Count)
                list.Add(null);

            for (var i = 0; i < list.Count; i++)
            {
                list[i] = (T) EditorGUILayout.ObjectField("Element " + i, list[i], typeof(T), allowSceneObject);
            }

            EditorGUI.indentLevel--;
        }
    }
}