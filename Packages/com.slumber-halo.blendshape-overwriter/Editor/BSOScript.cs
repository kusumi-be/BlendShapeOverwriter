using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using nadena.dev.ndmf;
using SlumberHalo.BlensShapeOverwriter;

/*
    指定されたシェイプキーを、引数のアニメーションにて上書きするスクリプト　の処理部分

    リップシンク用のシェイプキー（vrc.v_aaなど）を、別のアニメーションで上書きすることで、
    リップシンクと、口を小さくするなどの口を変形するシェイプキーの競合を防ぐことが、主な使用用途です

    こちらは処理のみを記述したスクリプトであり、UnityのEditor上で利用するのは"LipsyncRepair.cs"のほうです

    Ad-Hoc BlendShape Mix
    https://zatools.kb10uy.dev/ndmf-plugin/adhoc-blendshape-mix/
    を参考にしています
*/

[assembly: ExportsPlugin(typeof(BSOScript))]

namespace SlumberHalo.BlensShapeOverwriter
{
    public class BSOScript : Plugin<BSOScript>
    {
        // シェイプキーを表す構造体
        private struct ShapeKey{
            public string name; // シェイプキーの名前
            public float value; // シェイプキーの値
            public int index;   // 同じメッシュのうち、上から数えて何番目のシェイプキーかを表す、通し番号
        }

        /// <summary>
        /// 処理のメイン部分
        /// </summary>
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)  // 最適化の前の段階（MAの大抵の処理と同じ段階）で実行
                .BeforePlugin("com.anatawa12.avatar-optimizer") // AAOより先に実行する（上書きしたいシェイプキーの名前がAAOにより変えられると、正常に動作しないため）
                .Run("指定のシェイプキーを、指定のアニメーションの値で置換します", ctx => {

                    // Editorでアタッチする側のコンポーネントを取得
                    var component = ctx.AvatarRootObject.GetComponentInChildren<OverwriteBlendShape>();

                    // コンポーネントが取得できたなら
                    if (component != null)
                    {
                        // シェイプキーをアニメーションで上書き
                        OverwriteBlendShapesFromAnims(component.anims, component.shapeKeyNames, component.GetComponent<SkinnedMeshRenderer>());
                    }
                    UnityEngine.Object.DestroyImmediate(component); // 処理が終わったため、コンポーネントを削除
                });
        }

        /// <summary>
        /// 指定シェイプキーを、指定アニメーションで上書きする関数
        /// </summary>
        /// <param name="anims">シェイプキーを上書きするアニメーションの配列</param>
        /// <param name="shapeKeyNames">アニメーションにより上書きされるシェイプキーの配列</param>
        /// <param name="targetSkinnedMesh">上書きされるシェイプキーが存在するメッシュ</param>
        private void OverwriteBlendShapesFromAnims(AnimationClip[] anims, string[] shapeKeyNames, SkinnedMeshRenderer targetSkinnedMesh)
        {
            // 顔メッシュの取得
            Mesh mesh = targetSkinnedMesh.sharedMesh;

            // メッシュに存在するシェイプキー名のリストを取得
            List<string> shapeKeyNameList = GetBlendShapeNames(mesh);

            // アニメーションに含まれるシェイプキーの名前、値、インデックス（通し番号）を取得
            List<List<ShapeKey>> animShapeKeysList = new List<List<ShapeKey>>();
            for (int i = 0; i < anims.Length; i++) {
                // アニメーションのもつシェイプキーを保存
                List<ShapeKey> shapeKeys = GetShapeKeysFromAnimation(anims[i], shapeKeyNameList);
                animShapeKeysList.Add(shapeKeys);
            }

            // "shapeKeyNames"で指定されたシェイプキーを、アニメーションの値で上書き
            Mesh modifiedMesh = CreateOverwritedMesh(targetSkinnedMesh, animShapeKeysList, shapeKeyNames);   // シェイプキーをアニメーションで上書きした結果を、メッシュとして書き出し
            targetSkinnedMesh.sharedMesh = modifiedMesh;                                        // 元のメッシュを上書き
            ObjectRegistry.RegisterReplacedObject(mesh, modifiedMesh);                          // メッシュを置換したことを記録
        }

        /// <summary>
        /// メッシュに存在するシェイプキーの名前を返す関数
        /// </summary>
        /// <param name="mesh">シェイプキーの名前を取得したいメッシュ</param>
        /// <returns></returns>
        private List<string> GetBlendShapeNames(Mesh mesh)
        {
            return Enumerable.Range(0, mesh.blendShapeCount)
                .Select((i) => mesh.GetBlendShapeName(i))
                .ToList();
        }

        /// <summary>
        /// 引数のアニメーションにより動かされているシェイプキーの
        /// - 名前
        /// - frame 0でのキーの値
        /// を取得する関数
        /// </summary>
        /// <param name="animClip">動かしているシェイプキーを抽出されるアニメーション</param>
        /// <returns></returns>
        private List<ShapeKey> GetShapeKeysFromAnimation(AnimationClip animClip, List<string> shapeKeyNameList)
        {
            // アニメーションにより動かされているシェイプキーを保存する配列
            List<ShapeKey> shapeKeys = new List<ShapeKey>();

            // アニメーションが登録されていないなら、空の結果を返す
            if (animClip == null) {
                return shapeKeys;
            }

            // アニメーションを取得
            var bindings = AnimationUtility.GetCurveBindings(animClip);
            foreach (var binding in bindings)
            {
                // 0 frameのキーを取り出す
                var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                var key = curve.keys[0];

                // "シェイプキー"を動かすアニメーション"だけ"を取得
                // 該当アニメーションは"blendShape."で始まるため、該当するものを探す
                if (binding.propertyName.StartsWith("blendShape."))
                {
                    // "blendShape."はシェイプキー名には含まれないため、除外する
                    string shapeKeyName = binding.propertyName.Replace("blendShape.", "");

                    // 動かされているシェイプキーの名前とキーの値を保存
                    ShapeKey shapeKey = new ShapeKey();
                    shapeKey.name = shapeKeyName;   // 名前を保存
                    shapeKey.value = key.value;     // シェイプキーの数値を保存
                    shapeKey.index = shapeKeyNameList.IndexOf(shapeKeyName);    // シェイプキーがそのメッシュの上から何番目のシェイプキーかを保存
                    
                    shapeKeys.Add(shapeKey);
                }
            }

            return shapeKeys;
        }

        /// <summary>
        /// シェイプキーにアニメーションを焼きこんだ状態のメッシュを生成する関数
        /// </summary>
        /// <param name="skinnedMesh">元のメッシュ</param>
        /// <param name="animShapeKeysList">各アニメーションが保持するシェイプキーの値のリスト</param>
        /// <param name="targetShapeKeyNames">シェイプキーの名前の配列　アニメーションを焼きこまれる対象となる</param>
        /// <returns>"シェイプキーにアニメーションを焼きこんだ"状態のメッシュ</returns>
        private Mesh CreateOverwritedMesh(SkinnedMeshRenderer skinnedMesh, List<List<ShapeKey>> animShapeKeysList, string[] targetShapeKeyNames)
        {
            // やること
            // 1. アニメーションのシェイプキーを読み込み　引数でやってる
            // 2. 上書き対象のシェイプキーを読み込み(component.shapeKeyNameから)
            // 3. アニメーションのシェイプキーをすべて合成してメッシュをつくる
            // 4. 3のメッシュを、2のシェイプキーの結果として登録
            
            // シェイプキーによる、元のメッシュとの差分を記録する配列
            var mesh = skinnedMesh.sharedMesh;
            var numVert = mesh.vertexCount;                     // 頂点数
            Vector3[] positionsSource = new Vector3[numVert];   // 位置の差分
            Vector3[] normalsSource = new Vector3[numVert];     // 法線の差分
            Vector3[] tangentsSource = new Vector3[numVert];    // 接線の差分

            // アニメーションをシェイプキーに変換した結果を保存する配列
            Vector3[] positionsResult = new Vector3[numVert];   // 位置
            Vector3[] normalsResult = new Vector3[numVert];     // 法線
            Vector3[] tangentsResult = new Vector3[numVert];    // 接線

            // 加工用のメッシュを、元メッシュから複製
            var modifiedMesh = UnityEngine.Object.Instantiate(mesh);
            modifiedMesh.ClearBlendShapes();    // 一度全てのシェイプキーを削除

            // 上書き
            for (int index = 0; index < mesh.blendShapeCount; index++) {

                // 各シェイプキーが、上書き対象か確認
                var name = mesh.GetBlendShapeName(index);
                int pos = Array.IndexOf(targetShapeKeyNames, name); // targetShapeKeyNamesの中にnameが存在するなら、その位置を、存在しないなら-1を返す

                // シェイプキーが上書き対象の場合
                if (pos > -1) {
                    // アニメーションをシェイプキーに変換した結果を保存する配列の初期化
                    for (int k = 0; k < numVert; k++) {
                        positionsResult[k] = Vector3.zero;
                        normalsResult[k]  = Vector3.zero;
                        tangentsResult[k] = Vector3.zero;
                    }

                    // アニメーションに含まれる各シェイプキーごとに、その移動量を加算していく
                    foreach (var animShapeKey in animShapeKeysList[pos]) {
                        mesh.GetBlendShapeFrameVertices(animShapeKey.index, 0, positionsSource, normalsSource, tangentsSource); // シェイプキーによる元のメッシュとの差分配列を取得
                        var originValue = skinnedMesh.GetBlendShapeWeight(animShapeKey.index);                                  // そのシェイプキ―の元々の値を取得

                        // 頂点の分だけ繰り返す
                        for (int k = 0; k < numVert; k++) {
                            positionsResult[k] += positionsSource[k] * (animShapeKey.value - originValue) / 100;    // 位置の差分を記録
                            normalsResult[k]   += normalsSource[k]   * (animShapeKey.value - originValue) / 100;    // 法線の差分を記録
                            tangentsResult[k]  += tangentsSource[k]  * (animShapeKey.value - originValue) / 100;    // 接線の差分を記録
                        }
                    }

                    // 元のシェイプキーのかわりに、アニメーションにより生成したシェイプキーを追加
                    var maxWeight = mesh.GetBlendShapeFrameWeight(index, 0);
                    modifiedMesh.AddBlendShapeFrame(name, maxWeight, positionsResult, normalsResult, tangentsResult);
                }
                // シェイプキーが上書き対象でない場合
                else {
                    // 元のシェイプキーを読み込み
                    mesh.GetBlendShapeFrameVertices(index, 0, positionsResult, normalsResult, tangentsResult);

                    // シェイプキーをそのまま記録
                    var maxWeight = mesh.GetBlendShapeFrameWeight(index, 0);
                    modifiedMesh.AddBlendShapeFrame(name, maxWeight, positionsResult, normalsResult, tangentsResult);
                }
            }

            return modifiedMesh;
        }
    }
}
