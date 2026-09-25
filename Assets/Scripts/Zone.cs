using UnityEngine;

public class Zone : MonoBehaviour
{
    public enum ZoneType //Tipos de zonas existentes
    {
        Fertile,
        Arid,
        Normal
    }

    public ZoneType zonetype = ZoneType.Normal; //Zona actual (Predeterminada)
}
