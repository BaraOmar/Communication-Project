using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

public enum MuxModel
{
    [Display(Name = "Select a model")]
    Undefined = 0,

    [Display(Name = "OMS 1600")]
    Oms1600 = 1,

    [Display(Name = "OMS 1200")]
    Oms1200 = 2,

    [Display(Name = "OMS 800")]
    Oms800 = 3,

    [Display(Name = "SPO 1410")]
    Spo1410 = 4,

    [Display(Name = "Tellab")]
    Tellab = 5,

    [Display(Name = "XMP")]
    Xmp = 6
}

public enum MuxSection
{
    Standard = 0,
    Main = 1,
    Sub = 2
}