using Rowles.LeanCorpus.Tests.Shared.Infrastructure;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Rowles.LeanCorpus.Tests.Unit.Infrastructure;

/// <summary>
/// Contains unit tests for <see cref="DelayedMessageBus"/>.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class DelayedMessageBusTests
{
    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class SpyMessageBus : LongLivedMarshalByRefObject, IMessageBus
    {
        private readonly List<IMessageSinkMessage> _messages = [];
        public IReadOnlyList<IMessageSinkMessage> Messages => _messages;

        public bool QueueMessage(IMessageSinkMessage message)
        {
            _messages.Add(message);
            return true;
        }

        public void Dispose() { }
    }

    private sealed class StubMessage : LongLivedMarshalByRefObject, IMessageSinkMessage { }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "DelayedMessageBus: QueueMessage returns true")]
    public void QueueMessage_ReturnTrue()
    {
        var bus = new DelayedMessageBus(new SpyMessageBus());

        Assert.True(bus.QueueMessage(new StubMessage()));
    }

    [Fact(DisplayName = "DelayedMessageBus: QueueMessage does not forward to inner bus immediately")]
    public void QueueMessage_DoesNotForwardImmediately()
    {
        var inner = new SpyMessageBus();
        var bus = new DelayedMessageBus(inner);

        bus.QueueMessage(new StubMessage());

        Assert.Empty(inner.Messages);
    }

    [Fact(DisplayName = "DelayedMessageBus: Flush forwards all buffered messages to inner bus")]
    public void Flush_ForwardsAllBufferedMessages()
    {
        var inner = new SpyMessageBus();
        var bus = new DelayedMessageBus(inner);
        var m1 = new StubMessage();
        var m2 = new StubMessage();

        bus.QueueMessage(m1);
        bus.QueueMessage(m2);
        bus.Flush();

        Assert.Equal(2, inner.Messages.Count);
        Assert.Contains(m1, inner.Messages);
        Assert.Contains(m2, inner.Messages);
    }

    [Fact(DisplayName = "DelayedMessageBus: Flush clears the buffer so a second Flush sends nothing")]
    public void Flush_ClearsBuffer()
    {
        var inner = new SpyMessageBus();
        var bus = new DelayedMessageBus(inner);
        bus.QueueMessage(new StubMessage());
        bus.Flush();

        var countAfterFirstFlush = inner.Messages.Count;
        bus.Flush();

        Assert.Equal(countAfterFirstFlush, inner.Messages.Count);
    }

    [Fact(DisplayName = "DelayedMessageBus: Discard clears buffer without forwarding to inner bus")]
    public void Discard_ClearsWithoutForwarding()
    {
        var inner = new SpyMessageBus();
        var bus = new DelayedMessageBus(inner);
        bus.QueueMessage(new StubMessage());

        bus.Discard();
        bus.Flush();

        Assert.Empty(inner.Messages);
    }

    [Fact(DisplayName = "DelayedMessageBus: Dispose flushes pending messages")]
    public void Dispose_FlushesMessages()
    {
        var inner = new SpyMessageBus();
        var msg = new StubMessage();
        var bus = new DelayedMessageBus(inner);
        bus.QueueMessage(msg);

        bus.Dispose();

        Assert.Contains(msg, inner.Messages);
    }

    [Fact(DisplayName = "DelayedMessageBus: QueueMessage is thread-safe under concurrent access")]
    public void QueueMessage_ConcurrentAccess_AllMessagesBuffered()
    {
        var inner = new SpyMessageBus();
        var bus = new DelayedMessageBus(inner);
        const int count = 500;

        Parallel.For(0, count, _ => bus.QueueMessage(new StubMessage()));
        bus.Flush();

        Assert.Equal(count, inner.Messages.Count);
    }
}
