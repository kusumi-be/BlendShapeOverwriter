using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SlumberHalo.BlensShapeOverwriter
{
    public class BSOBaseEditor : Editor
    {
        // SerializedProperty
        protected SerializedProperty blendShapeNamesProperty; // シェイプキーの名前
        protected SerializedProperty animsProperty;           // アニメーションファイル

        protected List<string> allBlendShapeNames;            // シェイプキーの名前の配列
        protected HashSet<string> selectedBlendShapes;        // プルダウンにて選択されているシェイプキー

        // アタッチされた際の動作
        protected virtual void OnEnable()
        {
            // コンポーネントがついているオブジェクトを取得
            if (target != null) {
                OverwriteBlendShape overwriteBlendShape = (OverwriteBlendShape)target;

                // 対象のメッシュを取得
                SkinnedMeshRenderer mesh = overwriteBlendShape.GetComponent<SkinnedMeshRenderer>();
                
                // シェイプキー名を取得
                allBlendShapeNames = GetBlensShapeNames(mesh);
            }
        }

        // InspectorでのGUIの挙動を定義
        public override void OnInspectorGUI()
        {
            
        }

        /// <summary>
        /// メッシュに含まれるシェイプキーの名前を、配列として返す関数
        /// </summary>
        /// <param name="mesh">シェイプキー名を調べるメッシュ</param>
        /// <returns>シェイプキーの名前のリスト</returns>
        private List<string> GetBlensShapeNames(SkinnedMeshRenderer mesh)
        {
            List<string> blendShapeNames = new List<string>();
            for (int i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
            {
                var name = mesh.sharedMesh.GetBlendShapeName(i);
                blendShapeNames.Add(name);
            }

            return blendShapeNames;
        }

        /// <summary>
        /// GUI上にBlendShapeとAnimationClipの入力欄を描画する関数
        /// </summary>
        protected virtual void ShowBlendShapesAnimUI()
        {
            // 見出しを設定
            EditorGUILayout.BeginHorizontal();  // 水平に配置
            EditorGUILayout.LabelField("ShapeKey", GUILayout.Width(150));
            EditorGUILayout.LabelField("Animation");
            EditorGUILayout.EndHorizontal();    // 水平配置を終了
            
        }
    }
}
