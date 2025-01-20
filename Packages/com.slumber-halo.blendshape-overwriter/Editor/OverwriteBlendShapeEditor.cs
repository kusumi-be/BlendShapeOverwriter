using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SlumberHalo.BlensShapeOverwriter
{
    [CustomEditor(typeof(OverwriteBlendShape))]
    public class OverwriteBlendShapeEditor : BSOBaseEditor
    {
        // アタッチされた際の動作
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        // InspectorでのGUIの挙動を定義
        public override void OnInspectorGUI()
        {
            // SerializedPropertyを取得
            serializedObject.Update();
            base.blendShapeNamesProperty = serializedObject.FindProperty("shapeKeyNames");
            base.animsProperty = serializedObject.FindProperty("anims");

            // BlendShapeとAnimationの個数を同期
            SyncArraySize();
            
            // プルダウンにて選択されているシェイプキーを記録
            SavePulledDownBlendShape();

            // BlendShapeとAnimationClipの入力欄を描画
            ShowBlendShapesAnimUI();

            // ボタンを描画
            ShowButtonUI();

            // 変更後の値を保存
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///  BlendShapeとAnimationの個数を同期させる関数
        /// </summary>
        private void SyncArraySize()
        {
            if (blendShapeNamesProperty.arraySize != animsProperty.arraySize)
            {
                int newSize = Mathf.Min(blendShapeNamesProperty.arraySize, animsProperty.arraySize);
                blendShapeNamesProperty.arraySize = newSize;
                animsProperty.arraySize = newSize;
            }
        }

        /// <summary>
        /// GUI上にBlendShapeとAnimationClipの入力欄を描画する関数
        /// </summary>
        protected override void ShowBlendShapesAnimUI()
        {
            // 見出しを設定
            EditorGUILayout.BeginHorizontal();  // 水平に配置
            EditorGUILayout.LabelField("ShapeKey", GUILayout.Width(150));
            EditorGUILayout.LabelField("Animation");
            EditorGUILayout.EndHorizontal();    // 水平配置を終了
            
            // BlendShapeとAnimationClipの個数分繰り返す
            for (int i = 0; i < blendShapeNamesProperty.arraySize; i++)
            {
                // UIを水平に配置
                EditorGUILayout.BeginHorizontal();

                // まだ選択されていないシェイプキーだけを抽出（pull-downによる重複選択を防ぐため）
                List<string> availableBlendShapes = new List<string>(allBlendShapeNames);
                SerializedProperty blendShapeElement = blendShapeNamesProperty.GetArrayElementAtIndex(i);

                availableBlendShapes.RemoveAll(name => selectedBlendShapes.Contains(name) && name != blendShapeElement.stringValue);

                // BlendShapeのプルダウンを描画
                int selectedIndex = Mathf.Max(availableBlendShapes.IndexOf(blendShapeElement.stringValue), 0);
                selectedIndex = EditorGUILayout.Popup(selectedIndex, availableBlendShapes.ToArray(), GUILayout.Width(150));
                blendShapeElement.stringValue = availableBlendShapes[selectedIndex];

                // AnimationClipを描画
                SerializedProperty animElement = animsProperty.GetArrayElementAtIndex(i);
                AnimationClip clip = (AnimationClip)animElement.objectReferenceValue;
                animElement.objectReferenceValue = EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);

                // 水平配置を終了
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// GUI上にボタンを描画する関数
        /// </summary>
        private void ShowButtonUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Pair"))
            {
                // 未使用のシェイプキーを初期値として設定
                List<string> unusedBlendShapes = new List<string>(allBlendShapeNames);
                unusedBlendShapes.RemoveAll(name => selectedBlendShapes.Contains(name));
                if (unusedBlendShapes.Count > 0)
                {
                    blendShapeNamesProperty.arraySize++;
                    animsProperty.arraySize++;
                    SerializedProperty newBlendShapeElement = blendShapeNamesProperty.GetArrayElementAtIndex(blendShapeNamesProperty.arraySize - 1);
                    newBlendShapeElement.stringValue = unusedBlendShapes[0]; // 未使用の最初のシェイプキーを設定
                }
                else
                {
                    EditorUtility.DisplayDialog("追加できません", "利用可能な未使用のシェイプキーがありません。", "OK");
                }
            }
            if (GUILayout.Button("Remove Last Pair"))
            {
                if (blendShapeNamesProperty.arraySize > 0)
                {
                    blendShapeNamesProperty.arraySize--;
                    animsProperty.arraySize--;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// プルダウンにて選択されているシェイプキーを記録する関数
        /// </summary>
        private void SavePulledDownBlendShape()
        {
            selectedBlendShapes = new HashSet<string>();
            for (int i = 0; i < blendShapeNamesProperty.arraySize; i++)
            {
                SerializedProperty blendShapeElement = blendShapeNamesProperty.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(blendShapeElement.stringValue))
                {
                    selectedBlendShapes.Add(blendShapeElement.stringValue);
                }
            }
        }
    }
}
