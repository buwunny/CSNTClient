namespace NTClient
{
  public class Topic {
    public string name = "";
    public int uid = -1;
    public string type = "string";
    public string[] properties = new string[0];

  
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
      Dictionary<string, int> dict = new Dictionary<string, int>();
      dict["Uid"] = uid;
      return dict;
    }
  }
}