using AtmSimulator.Patterns.State;
using FluentAssertions;

namespace AtmSimulator.Tests.Patterns
{
    public class AtmStatePatternTests
    {
        [Fact]
        public void InitialState_IsNoCardState()
        {
            var atm = new AtmContext();

            atm.CurrentState.Should().BeOfType<NoCardState>();
            atm.CurrentState.CanInsertCard().Should().BeTrue();
            atm.CurrentState.CanEnterPin().Should().BeFalse();
            atm.CurrentState.CanPerformTransaction().Should().BeFalse();
            atm.CurrentState.CanEjectCard().Should().BeFalse();
        }

        [Fact]
        public void InsertCard_TransitionsToCardInsertedState()
        {
            var atm = new AtmContext();
            atm.InsertCard("1234567890123456");

            atm.CurrentState.Should().BeOfType<CardInsertedState>();
            var state = (CardInsertedState)atm.CurrentState;
            state.CardNumber.Should().Be("1234567890123456");
            state.CanEnterPin().Should().BeTrue();
            state.CanInsertCard().Should().BeFalse();
            state.CanPerformTransaction().Should().BeFalse();
            state.CanEjectCard().Should().BeTrue();
        }

        [Fact]
        public void InsertCard_WhenCardAlreadyInserted_ThrowsInvalidOperationException()
        {
            var atm = new AtmContext();
            atm.InsertCard("1234567890123456");

            var act = () => atm.InsertCard("9999999999999999");

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Неможливо вставити картку*");
        }

        [Fact]
        public void Authenticate_WhenCardInserted_TransitionsToAuthenticatedState()
        {
            var atm = new AtmContext();
            atm.InsertCard("1234567890123456");
            atm.Authenticate();

            atm.CurrentState.Should().BeOfType<AuthenticatedState>();
            var state = (AuthenticatedState)atm.CurrentState;
            state.CardNumber.Should().Be("1234567890123456");
            state.CanPerformTransaction().Should().BeTrue();
            state.CanInsertCard().Should().BeFalse();
            state.CanEnterPin().Should().BeFalse();
            state.CanEjectCard().Should().BeTrue();
        }

        [Fact]
        public void Authenticate_WhenNoCard_ThrowsInvalidOperationException()
        {
            var atm = new AtmContext();

            var act = () => atm.Authenticate();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Неможливо пройти аутентифікацію*");
        }

        [Fact]
        public void EjectCard_FromAuthenticatedState_TransitionsToNoCardState()
        {
            var atm = new AtmContext();
            atm.InsertCard("1234567890123456");
            atm.Authenticate();

            atm.EjectCard();

            atm.CurrentState.Should().BeOfType<NoCardState>();
        }

        [Fact]
        public void EnsureCanTransact_WhenAuthenticated_DoesNotThrow()
        {
            var atm = new AtmContext();
            atm.InsertCard("1234567890123456");
            atm.Authenticate();

            var act = () => atm.EnsureCanTransact();

            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureCanTransact_WhenNotAuthenticated_ThrowsInvalidOperationException()
        {
            var atm = new AtmContext();

            var act = () => atm.EnsureCanTransact();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Неможливо виконати операцію*");
        }
    }
}
