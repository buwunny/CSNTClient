﻿namespace NTClient
{
  public class Topic {
    private int id = 0; // 
    private string name = "";
    private int pubUid = -1;
    private string type = "string";
    private object value = new();
    private Dictionary<string, object> properties = new Dictionary<string, object>();

    
    public Topic(string name, int uid, string type, Dictionary<string, object> properties)
    {
      this.name = name;
      this.pubUid = uid;
      this.type = type;
      this.properties = properties;
    }
    public Topic()
    {
    }

    public int Id
    {
      get { return id; }
      set { id = value; }
    }

    public string Name
    {
      get { return name; }
      set { name = value; }
    }

    public int PubUid
    {
      get { return pubUid; }
      set { pubUid = value; }
    }

    public string Type
    {
      get { return type; }
      set { type = value; }
    }

    public object Value
    {
      get { return value; }
      set { this.value = value; }
    }

    public Dictionary<string, object> Properties
    {
      get { return properties; }
      set { properties = value; }
    }

    public Dictionary<string, object> GetPublishObject()
    {
      Dictionary<string, object> dict = new Dictionary<string, object>()
      {
        { "name", name },
        { "pubuid", pubUid },
        { "type", type },
        { "properties", properties }
      };
      return dict;
    }

    public Dictionary<string, int> GetUnpublishObject()
    {
      Dictionary<string, int> dict = new Dictionary<string, int>()
      {
        { "pubuid", pubUid }
      };
      return dict;
    }
  }
}