using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TaikoAssist
{
    public class FPS : Singleton<FPS>
    {
        public Text FPSText;
        private Queue<float> Frame = new Queue<float>();

        void Update()
        {
            Frame.Enqueue(Time.deltaTime);
            while (Frame.Sum() > 1)
            {
                Frame.Dequeue();
            }
            FPSText.text = $"FPS: {Frame.Count}";
        }
    }
}
