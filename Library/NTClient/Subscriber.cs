namespace NTClient
{
  public class Subscriber
  {
    private string[] topics;
    private int uid;
    private SubscriptionOptions options;

    public Subscriber(string[] topicNames, int uid, SubscriptionOptions options)
    {
      this.topics = topicNames;
      this.uid = uid;
      this.options = options;
    }

    public Subscriber(string[] topicNames, int uid)
    {
      this.topics = topicNames;
      this.uid = uid;
      this.options = new SubscriptionOptions();
    }

    public string[] Topics
    {
      get { return topics; }
      set { topics = value; }
    }

    public int Uid
    {
      get { return uid; }
      set { uid = value; }
    }

    public SubscriptionOptions Options
    {
      get { return options; }
      set { options = value; }
    }

    public Dictionary<string, object> GetSubscribeObject()
    {
      var subscribe = new Dictionary<string, object>
      {
      { "topics", topics},
      { "subuid", uid },
      { "options", options }
      };
    
      return subscribe;
    }

    public Dictionary<string, object> GetUnsubscribeObject()
    {
      var unubscribe = new Dictionary<string, object>
      {
        { "subuid", uid },
      };
      return unubscribe;
    }
  }

  public class SubscriptionOptions
  {
    private double periodic = 0.1;
    private bool all = false;
    private bool topicsOnly = false;
    private bool prefix = false;

    public SubscriptionOptions(double periodic, bool all, bool topicsOnly, bool prefix)
    {
      this.periodic = periodic;
      this.all = all;
      this.topicsOnly = topicsOnly;
      this.prefix = prefix;
    }

    public SubscriptionOptions()
    {
    }

    public double Periodic
    {
      get { return periodic; }
      set { periodic = value; }
    }

    public bool All
    {
      get { return all; }
      set { all = value; }
    }

    public bool TopicsOnly
    {
      get { return topicsOnly; }
      set { topicsOnly = value; }
    }

    public bool Prefix
    {
      get { return prefix; }
      set { prefix = value; }
    }
  }
}
