using System;
using System.Data.Entity; 
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class UserAccountData
    {
        public bool EmailExists(string email)
        {
            bool emailExists = false;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                emailExists = dataBaseContext.ACCOUNT.Any(ua => ua.EMAIL == email);
            }

            return emailExists;
        }

        public (ACCOUNT account, USER_PROFILE profile) CreateAccount(CreateAccountArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction()) 
            {

                var profille = new USER_PROFILE
                {
                    DISPLAYNAME = args.DisplayName,
                    ISACTIVE = true,
                    CREATEDATUTC = args.CreationDate
                };

                dataBaseContext.USER_PROFILE.Add(profille);
                dataBaseContext.SaveChanges();

                var account = new ACCOUNT
                {
                    USERID = profille.USERID,
                    EMAIL = args.Email,
                    PASSWORD = args.Password,
                    ISEMAILVERIFIED = false,
                    CREATEDATUTC = args.CreationDate,
                    UPDATEDATUTC = args.CreationDate
                };

                dataBaseContext.ACCOUNT.Add(account);
                dataBaseContext.SaveChanges();
                transaction.Commit();

                return (account, profille);
            }
        }

        public ACCOUNT GetAccountById(long accountId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.ACCOUNT.SingleOrDefault(a => a.ACCOUNTID == accountId);
            }
        }

        public void MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var emailVerified = dataBaseContext.ACCOUNT.Single(a => a.ACCOUNTID == accountId);
                emailVerified.ISEMAILVERIFIED = true;
                emailVerified.UPDATEDATUTC = nowUtc;
                dataBaseContext.SaveChanges();
            }
        }
    }
}
