using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public class Viewport : MonoBehaviour
    {
        public static Viewport instance;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
        }
    }
}
