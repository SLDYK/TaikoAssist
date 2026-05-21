using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TaikoAssist
{
    public class FPS : MonoBehaviour
    {
        public Text FrameRate;
        private Queue<float> frameTimes = new Queue<float>();

        void Update()
        {
            frameTimes.Enqueue(Time.deltaTime);
            while (frameTimes.Sum() > 1)
            {
                frameTimes.Dequeue();
            }
            FrameRate.text = $"FPS: {frameTimes.Count}";
        }
    }
}