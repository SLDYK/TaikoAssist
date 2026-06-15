
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using UnityEditorInternal;

namespace TaikoAssist
{
    [CustomEditor(typeof(MeshRenderer)), CanEditMultipleObjects]
    public class MeshRendererEditorCustom : Editor
    {

        //Armazena Sorting Layer criadas no unity
        private string[] sortingLayerNames;

        //Order
        private int sortingOrder;

        //Layer
        private int sortingLayer;

        //Objetos selecionados
        private MeshRenderer[] renderers;

        //Se todos os objetos selecionado possuem os mesmos valores
        private bool sortingLayerEqual;
        private bool sortingOrderEqual;


        void OnEnable()
        {
            //Cache de Sorting Layer criadas.
            sortingLayerNames = GetSortingLayerNames();

            //Recupera objetos selecionados
            System.Object[] objects = serializedObject.targetObjects;

            if (objects == null || objects.Length == 0) return;

            //Armazena valores iniciais
            MeshRenderer first = objects[0] as MeshRenderer;
            if (first == null) return;

            sortingOrder = first.sortingOrder;
            string layerName = first.sortingLayerName;
            sortingLayer = Mathf.Max(System.Array.IndexOf(sortingLayerNames, layerName), 0);

            //Cast
            renderers = new MeshRenderer[objects.Length];
            //Igualdade entre multiobjects
            sortingLayerEqual = true;
            sortingOrderEqual = true;
            for (int i = 0; i < objects.Length; i++)
            {
                //Cast
                renderers[i] = objects[i] as MeshRenderer;
                //Verifica se todos os objetos possuem o mesmo valor
                if (renderers[i].sortingOrder != sortingOrder) sortingOrderEqual = false;
                if (renderers[i].sortingLayerName != layerName) sortingLayerEqual = false;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sorting Layer Settings", EditorStyles.boldLabel);

            /**
             * SORTING Layer
             **/
            EditorGUI.BeginChangeCheck();

            //UI
            EditorGUI.showMixedValue = !sortingLayerEqual;
            sortingLayer = EditorGUILayout.Popup("Sorting Layer", sortingLayer, sortingLayerNames);

            //Aplicar modificacoes e igualar valores
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                foreach (MeshRenderer r in renderers)
                {
                    r.sortingLayerName = sortingLayerNames[sortingLayer];
                    EditorUtility.SetDirty(r);
                }
                sortingLayerEqual = true;
                serializedObject.ApplyModifiedProperties();
            }


            /**
             * SORTING ORDER
             **/
            EditorGUI.BeginChangeCheck();

            //UI
            EditorGUI.showMixedValue = !sortingOrderEqual;
            sortingOrder = EditorGUILayout.IntField("Order in Layer", sortingOrder);

            //Aplicar modificacoes e igualar valores
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                foreach (MeshRenderer r in renderers)
                {
                    r.sortingOrder = sortingOrder;
                    EditorUtility.SetDirty(r);
                }
                sortingOrderEqual = true;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public string[] GetSortingLayerNames()
        {
            // 使用公共 API 获取层级名称，比反射更安全且兼容新版本
            SortingLayer[] layers = SortingLayer.layers;
            string[] names = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
            }
            return names;
        }

    }
}



