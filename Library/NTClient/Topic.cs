namespace NTClient
{
  public class Topic {
    private string name = "";
    private int uid = -1;
    private string type = "string";
    private Dictionary<string, object> properties = new Dictionary<string, object>();

    
     public Topic(string name , int uid, string type, Dictionary<string, object> properties)
    {
      this.name = name;
      this.uid = uid;
      this.type = type;
      this.properties = properties;
    }
    public Topic()
    {
    }

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

    public Dictionary<string, object> Properties
    {
      get { return properties; }
      set { properties = value; }
    }
   
    public Dictionary<string, object> ForPublish()
    {
      Dictionary<string, object> dict = new Dictionary<string, object>()
      {
        { "name", name },
        { "pubuid", uid },
        { "type", type },
        { "properties", properties }
      };
      return dict;
    }

    public Dictionary<string, int> ForUnpublish()
    {
      Dictionary<string, int> dict = new Dictionary<string, int>()
      {
        { "pubuid", uid }
      };
      return dict;
    }
  }
}