using UnityEngine;

/*
    指定されたシェイプキーを、引数のアニメーションにて上書きするスクリプト　の公開部分

    リップシンク用のシェイプキー（vrc.v_aaなど）を、別のアニメーションで上書きすることで、
    リップシンクと、口を小さくするなどの口を変形するシェイプキーの競合を防ぐことが、主な使用用途です
    
    このスクリプトをskinned mesh rendererにアタッチすると、利用できます

    Ad-Hoc BlendShape Mix
    https://zatools.kb10uy.dev/ndmf-plugin/adhoc-blendshape-mix/
    を参考にしています
*/

namespace SlumberHalo.BlensShapeOverwriter{
    [RequireComponent(typeof(SkinnedMeshRenderer))]                     // skinned mesh rendererがあるかの確認
    [AddComponentMenu("BlensShape Overwriter/BSO Overwrite Lipsync")]   // AddComponentでどこに表示されるかを指定
    [DisallowMultipleComponent]                                         // 一つのメッシュに2つ以上つけることを禁止する（予期せぬエラーを避けるため）

    public class OverwriteLipsync : OverwriteBlendShape
    {
    }
}

