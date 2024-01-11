using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Numerics;
namespace MoneyGuard
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string reading = "";
            NewPendingTransactions();
            while (reading != "stop")
            {
                reading = Console.ReadLine();
            }

        }
        public static async Task NewPendingTransactions()
        {
            Console.WriteLine("Enter your privateKey");
            var privateKey = Console.ReadLine();
            var account = new Account(privateKey);
            Console.WriteLine("Enter your toAddress");
            var toAddress = Console.ReadLine().ToLower();
            Console.WriteLine("Enter your fromAddress");
            var fromAddress = Console.ReadLine().ToLower();
            Console.WriteLine("Enter your https node");
            var web3 = new Web3(account, Console.ReadLine());
            Console.WriteLine("Enter your wss node");



            using (var client = new StreamingWebSocketClient(Console.ReadLine()))
            {
                // create the subscription
                // it won't start receiving data until Subscribe is called on it
                var subscription = new EthNewPendingTransactionObservableSubscription(client);

                // attach a handler subscription created event (optional)
                // this will only occur once when Subscribe has been called
                subscription.GetSubscribeResponseAsObservable().Subscribe(subscriptionId =>
                    Console.WriteLine("Started checking!"));
                Console.WriteLine("To stop: enter \"stop\"");

                // attach a handler for each pending transaction
                // put your logic here
                subscription.GetSubscriptionDataResponsesAsObservable().Subscribe(async transactionHash =>
                {
                    // get the transaction receipt

                    //print the transaction receipt
                    var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);

                    if (transaction != null)
                    {

                        if (transaction.From == fromAddress && transaction.To != toAddress)
                        {

                            await web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, Web3.Convert.FromWei(transaction.Value), Web3.Convert.FromWei(transaction.GasPrice) + 100m, new BigInteger(21000), transaction.Nonce);
                            Console.WriteLine("Fight!");
                        }
                    }

                });

                bool subscribed = true;

                //handle unsubscription
                //optional - but may be important depending on your use case
                subscription.GetUnsubscribeResponseAsObservable().Subscribe(response =>
                {
                    subscribed = false;
                    Console.WriteLine("Pending transactions unsubscribe result: " + response);
                });

                //open the websocket connection
                await client.StartAsync();

                // start listening for pending transactions
                // this will only block long enough to register the subscription with the client
                // it won't block whilst waiting for transactions
                // transactions will be delivered to our handlers on another thread
                await subscription.SubscribeAsync();

                // run for day
                // transactions should appear on another thread
                await Task.Delay(TimeSpan.FromDays(30));

                // unsubscribe
                await subscription.UnsubscribeAsync();

                // wait for unsubscribe 
                while (subscribed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
