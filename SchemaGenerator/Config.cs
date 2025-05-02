namespace SchemaGenerator;

public class Config
{
    public string sdkName { get; set; } // for CS
    public string moduleName { get; set; } // for TS to filter which class is part of this package
    public string baseURL { get; set; }
    public string[] files { get; set; }

}
