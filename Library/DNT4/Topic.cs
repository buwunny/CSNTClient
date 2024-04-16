namespace DNT4
{
  public class Topic {
    private string name = "";
    private int uid = -1;
    private string type = "";
    private string[] properties = new string[0];

    public string Name
    {
      get { return name; }
      set { name = value; }
    }

    public int Uid
    {
      get { return uid; }
      set { uid = value; }
    }

    public string Type
    {
      get { return type; }
      set { type = value; }
    }

    public string[] Properties
    {
      get { return properties; }
      set { properties = value; }
    }

    public Dictionary<string, object> ForPublish()
    {
      Dictionary<string, object> dict = new Dictionary<string, object>();
      dict["Name"] = Name;
      dict["Uid"] = Uid;
      dict["Type"] = Type;
      dict["Properties"] = Properties;
      return dict;
    }

    public Dictionary<string, int> ForUnpublish()
    {
      Dictionary<string, int> dict = new Dictionary<string, int>();
      dict["Uid"] = Uid;
      return dict;
    }
  }
}