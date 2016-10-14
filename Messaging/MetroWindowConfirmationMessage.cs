
namespace BacklogMaintainer.Messaging
{
    using Livet.Messaging;
    using MahApps.Metro.Controls.Dialogs;
    using System;
    using System.Windows;

    public class MetroWindowConfirmationMessage : ResponsiveInteractionMessage<bool?>
    {
        public MetroWindowConfirmationMessage(string text, string caption, MessageDialogStyle setting, Action<MessageDialogResult> callback, string messageKey)
            : base(messageKey)
        {
            this.Text = text;
            this.Caption = caption;
            this.Setting = setting;
            this.CallBack = callback;
        }

        /// <summary>
        /// 表示するメッセージを指定、または取得します。
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MetroWindowConfirmationMessage), new PropertyMetadata(null));


        /// <summary>
        /// キャプションを指定、または取得します。
        /// </summary>
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(MetroWindowConfirmationMessage), new PropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public MessageDialogStyle Setting
        {
            get { return (MessageDialogStyle)GetValue(SettingProperty); }
            set { SetValue(SettingProperty, value); }
        }

        public static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register("Setting", typeof(MessageDialogStyle), typeof(MetroWindowConfirmationMessage), new PropertyMetadata(MessageDialogStyle.Affirmative));

        /// <summary>
        /// 
        /// </summary>
        public Action<MessageDialogResult> CallBack
        {
            get { return (Action<MessageDialogResult>)GetValue(CallBackProperty); }
            set { SetValue(CallBackProperty, value); }
        }

        public static readonly DependencyProperty CallBackProperty =
            DependencyProperty.Register("CallBack", typeof(Action<MessageDialogResult>), typeof(MetroWindowConfirmationMessage), new PropertyMetadata(null));

        /// <summary>
        /// 派生クラスでは必ずオーバーライドしてください。Freezableオブジェクトとして必要な実装です。<br/>
        /// 通常このメソッドは、自身の新しいインスタンスを返すように実装します。
        /// </summary>
        /// <returns>自身の新しいインスタンス</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new MetroWindowConfirmationMessage(Text, Caption, Setting, CallBack, MessageKey);
        }
    }
}
