using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace lethalCompanyRevive.Misc
{
    public class TPObject : MonoBehaviour
    {
        public static TPObject? Instance { get; private set; }

        public MultiObjectPool<ShipTeleporter> ShipTeleporters { get; private set; }

        void Awake()
        {
            this.ShipTeleporters = new(this);

            TPObject.Instance = this;
        }
    }
}
