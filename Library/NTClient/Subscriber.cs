namespace NTClient
{
  public class Subscriber
  {
    private string[] topics;
    private int uid;
    private SubscriptionOptions options = new SubscriptionOptions();

    public Subscriber(string[] topics, int uid, SubscriptionOptions options)
    {
      this.topics = topics;
      this.uid = uid;
      this.options = options;
    }

    public Subscriber(string topic, int uid, SubscriptionOptions options)
    {
      topics = [topic];
      this.uid = uid;
      this.options = options;
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

    public Dictionary<string, object> GetSubscribeObject(bool includeOptions = false)
    {
      var subscribe = new Dictionary<string, object>
      {
      { "topics", topics},
      { "subuid", uid }
      };

      if (includeOptions)
      {
      subscribe.Add("options", options.ToOptions());
      }
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



    public Dictionary<string, object> ToOptions()
    {
      var options = new Dictionary<string, object>
      {
        { "periodic", periodic },
        { "all", all },
        { "topicsonly", topicsOnly },
        { "prefix", prefix }
      };

      return options;
    }
  }
}
