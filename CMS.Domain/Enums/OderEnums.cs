namespace CMS.Domain.Enums
{
    public enum TechStatus
    {
        current,
        old
    }
    public enum OrderSource
    {
        Facebook_صفحة,
        WhatsApp,
        اتصال_مباشر,
        Market_place
    }
    public enum OrderType
    {
        جملة,
        تركيب,
        نص_جملة
    }
    public enum OrderStatus
    {
        جديد,
        قيد_التنفيذ,
        تام,
        مؤجل,
        ملغي,
        محذوف,
        تم_التركيب,
    }
}
