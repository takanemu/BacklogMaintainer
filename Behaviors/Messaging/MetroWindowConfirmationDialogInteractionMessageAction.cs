
namespace BacklogMaintainer.Behaviors.Messaging
{
    using BacklogMaintainer.Messaging;
    using Livet.Behaviors.Messaging;
    using Livet.Messaging;
    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;
    using System.Windows;

    public class MetroWindowConfirmationDialogInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override async void InvokeAction(InteractionMessage message)
        {
            var confirmMessage = message as MetroWindowConfirmationMessage;

            if (confirmMessage != null)
            {
                var owner = base.AssociatedObject as MetroWindow;

                if(owner != null)
                {
                    var result = await owner.ShowMessageAsync(confirmMessage.Caption, confirmMessage.Text, confirmMessage.Setting);

                    confirmMessage.CallBack(result);
                }
            }
        }
    }
}
