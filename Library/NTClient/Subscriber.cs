namespace NTClient
{
  public class Subscriber
  {
    private Topic[] topics;
    private int uid;
    private SubscriptionOptions options = new SubscriptionOptions();

    public Subscriber(Topic[] topics, int uid, SubscriptionOptions options)
    {
      this.topics = topics;
      this.uid = uid;
      this.options = options;
    }

    public Topic[] Topics
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

    public Dictionary<string, object> ToSubscribe()
    {
      var subscribe = new Dictionary<string, object>
      {
        { "topics", topics},//topics.Select(t => t.Name).ToArray() },
        { "subuid", uid },
        { "options", options.ToOptions() }
      };
      return subscribe;

;
    }

    public Dictionary<string, object> ToUnsubscribe()
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
