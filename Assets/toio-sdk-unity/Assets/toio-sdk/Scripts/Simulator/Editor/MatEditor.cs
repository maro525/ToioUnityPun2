using UnityEditor;

#if UNITY_EDITOR
namespace toio.Simulator
{

    [CustomEditor(typeof(Mat))]
    public class MyScriptEditor : Editor
    {
        Mat.MatType lstMatType;
        int lstXMin, lstXMax, lstYMin, lstYMax;

        void OnEnable()
        {
            EditorUtility.SetDirty(target as Mat);
            var mat = target as Mat;
            this.lstMatType = mat.matType;
            this.lstXMin = mat.xMin; this.lstXMax = mat.xMax;
            this.lstYMin = mat.yMin; this.lstYMax = mat.yMax;
            mat.ApplyMatType();
        }

        public override void OnInspectorGUI()
        {
            var mat = target as Mat;

            mat.matType = (Mat.MatType)EditorGUILayout.EnumPopup("Mat Type", mat.matType);
            if (mat.matType != this.lstMatType){
                this.lstMatType = mat.matType;
                mat.ApplyMatType();
            }

            if (mat.matType == Mat.MatType.Custom){
                mat.xMin = (int)EditorGUILayout.IntSlider("x Min", mat.xMin, 0, mat.xMax-10);
                mat.xMax = (int)EditorGUILayout.IntSlider("x Max", mat.xMax, mat.xMin+10, 10000);
                mat.yMin = (int)EditorGUILayout.IntSlider("y Min", mat.yMin, 0, mat.yMax-10);
                mat.yMax = (int)EditorGUILayout.IntSlider("y Max", mat.yMax, mat.yMin+10, 10000);
            }
            if (this.lstXMin != mat.xMin || this.lstXMax != mat.xMax ||
                this.lstYMin != mat.yMin || this.lstYMax != mat.yMax)
            {
                this.lstXMin = mat.xMin; this.lstXMax = mat.xMax;
                this.lstYMin = mat.yMin; this.lstYMax = mat.yMax;
                mat.ApplyMatType();
            }
        }

    }

}
#endif