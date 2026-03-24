using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    public class BaseToggle : BaseSelect, ISelectBoolCache
    {
        [SerializeField]
        [ReadOnly]
        protected bool _isSelect = false;
        public bool IsSelect => _isSelect;
        public override void Select(bool isSelect)
        {
            base.Select(isSelect);
            _isSelect = isSelect;
        }
        public void SelectInvert()
        {
            DoSelectInvert();
        }
        void DoSelectInvert()
        {
            _isSelect = !_isSelect;
            Select(_isSelect);
        }
    }
}
