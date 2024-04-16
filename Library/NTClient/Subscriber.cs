namespace NTClient
{
  public class Subscriber
  {
    private Topic[] topics = new Topic[0];
    private int uid = -1;
    private SubscriptionOptions options = new SubscriptionOptions();

    // public Subscriber(Topic[] topics, int uid, SubscriptionOptions options)
    // {
    //   this.topics = topics;
    //   this.uid = uid;
    //   this.options = options;
    // }

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

    public struct Subscribe
    {
      public Topic[] topics;
      public int subuid;
      public SubscriptionOptions.Options options;
    }

    public Subscribe ToSubscribe()
    {
      Subscribe sub = new Subscribe();
      sub.topics = topics;
      sub.subuid = uid;
      sub.options = options.ToOptions();
      return sub;
    }

    public struct Unsubscribe
    {
      public int subuid;
    }

    public Unsubscribe ToUnsubscribe()
    {
      Unsubscribe unsub = new Unsubscribe();
      unsub.subuid = uid;
      return unsub;
    }
  }

  public class SubscriptionOptions
  {
    private double periodic = 0.1;
    private bool all = false;
    private bool topicsOnly = false;
    private bool prefix = false;

    public struct Options
    {
      public double periodic;
      public bool all;
      public bool topicsOnly;
      public bool prefix;
    }

    public Options ToOptions()
    {
      Options opt = new Options();
      opt.periodic = periodic;
      opt.all = all;
      opt.topicsOnly = topicsOnly;
      opt.prefix = prefix;
      return opt;
    }
  }
}
