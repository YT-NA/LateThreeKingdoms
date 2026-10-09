using System;

namespace Husamguk.Data
{
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceDef resource;
        public int amount;

        public ResourceAmount(ResourceDef resource, int amount)
        {
            this.resource = resource;
            this.amount = amount;
        }
    }
}
