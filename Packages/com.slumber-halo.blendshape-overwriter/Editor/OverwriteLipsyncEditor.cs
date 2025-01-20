using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
// using Cysharp.Threading.Tasks;

namespace SlumberHalo.BlensShapeOverwriter
{
    [CustomEditor(typeof(OverwriteLipsync))]
    public class OverwriteLipsyncEditor : BSOBaseEditor
    {
        private bool hasRipsyncShapekey = true;
        protected override void OnEnable()
        {
            // ベースとなる処理を実行
            // メッシュに含まれるシェイプキーの名前を取得
            base.OnEnable();

            // リップシンクに関連のあるシェイプキーのみ抜き出し
            List<string> filteredBlendShapeNames =  allBlendShapeNames.Where(x => x.StartsWith("vrc.v_") || x.StartsWith("v_"))
                                                                      .ToList();

            // フィルタリング結果が空の場合の対応
            if (filteredBlendShapeNames.Count == 0)
            {
                hasRipsyncShapekey = false;
                return;
            }
            else
            {
                hasRipsyncShapekey = true;

                // シリアライズされたプロパティを取得
                blendShapeNamesProperty = serializedObject.FindProperty("shapeKeyNames");
                animsProperty = serializedObject.FindProperty("anims");

                // シェイプキー、アニメーションの個数をリップシンクシェイプキーの数で上書き
                blendShapeNamesProperty.arraySize = filteredBlendShapeNames.Count;
                animsProperty.arraySize = filteredBlendShapeNames.Count;

                for (int i = 0; i < filteredBlendShapeNames.Count; i++)
                {
                    // リップシンク用シェイプキーのみをEditor上に表示するように設定
                    SerializedProperty blendShapeElement = blendShapeNamesProperty.GetArrayElementAtIndex(i);
                    blendShapeElement.stringValue = filteredBlendShapeNames[i];

                    // アニメーション入力欄の初期値を設定
                    SerializedProperty animElement = animsProperty.GetArrayElementAtIndex(i);
                    if (animElement.objectReferenceValue == null)
                    {
                        animElement.objectReferenceValue = null;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        public override void OnInspectorGUI()
        {
            // リップシンク用シェイプキ―がないメッシュにアタッチされた場合の例外処理
            if(hasRipsyncShapekey == false) {
                EditorGUILayout.HelpBox("リップシンク用の \"vrc.v_\" or \"v._\" で名前が始まるシェイプキーが存在しません\n顔メッシュ以外に設定する場合、かわりに\"BSO Overwrite BlendShape\"を利用してください", MessageType.Error);
                return;
            }

            // SerializedPropertyを取得
            serializedObject.Update();
            blendShapeNamesProperty = serializedObject.FindProperty("shapeKeyNames");
            animsProperty = serializedObject.FindProperty("anims");

            // 見出しを設定
            EditorGUILayout.BeginHorizontal();  // 水平に配置
            EditorGUILayout.LabelField("ShapeKey", GUILayout.Width(100));
            EditorGUILayout.LabelField("Animation");
            EditorGUILayout.EndHorizontal();    // 水平配置を終了

            // GUI上にBlendShapeとAnimationClipの入力欄を描画
            ShowBlendShapesAnimUI();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// GUI上にBlendShapeとAnimationClipの入力欄を描画する関数
        /// </summary>
        protected override void ShowBlendShapesAnimUI()
        {
            for (int i = 0; i < blendShapeNamesProperty.arraySize; i++)
            {
                // 水平に配置
                EditorGUILayout.BeginHorizontal();

                // リップシンク用シェイプキー名を描画
                SerializedProperty blendShapeElement = blendShapeNamesProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(blendShapeElement.stringValue, GUILayout.Width(100));

                // AnimationClipの入力欄を描画
                SerializedProperty animElement = animsProperty.GetArrayElementAtIndex(i);
                AnimationClip clip = (AnimationClip)animElement.objectReferenceValue;
                animElement.objectReferenceValue = EditorGUILayout.ObjectField( clip, typeof(AnimationClip), false);

                // 水平配置を終了
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
