namespace Prg.PubSub
{
    public class Notification<T>
    {
        public readonly string Subject;
        public readonly T Value;

        public Notification(string subject, T value = default)
        {
            Subject = subject;
            Value = value;
        }
    }
}
