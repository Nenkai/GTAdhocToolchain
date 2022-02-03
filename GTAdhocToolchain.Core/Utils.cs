using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core
{
    public class Utils
    {
        public static string OperatorNameToPunctuator(string @operator)
        {
            if (string.IsNullOrEmpty(@operator))
                throw new Exception("Tried to call GetHumanReadable while name was null. Did you even call deserialize?");

            switch (@operator)
            {
                case "__elem__":
                    return "[]";

                case "__eq__":
                    return "==";
                case "__ge__":
                    return ">=";
                case "__gt__":
                    return ">";
                case "__le__":
                    return "<=";
                case "__lt__":
                    return "<";

                case "__invert__":
                    return "~";
                case "__lshift__":
                    return "<<";

                case "__mod__":
                    return "%";

                case "__ne__":
                    return "!=";

                case "__not__":
                    return "!";

                case "__or__":
                    return "|";

                case "__post_decr__":
                    return "@--";
                case "__post_incr__":
                    return "@++";

                case "__pre_decr__":
                    return "--@";
                case "__pre_incr__":
                    return "++@";

                case "__pow__":
                    return "** (power)";

                case "__rshift__":
                    return ">>";

                case "__minus__":
                    return "-";

                case "__uminus__":
                    return "-@";

                case "__uplus__":
                    return "+@";

                case "__xor__":
                    return "^";

                case "__div__":
                    return "/";
                case "__mul__":
                    return "*";
                case "__add__":
                    return "+";
                case "__min__":
                    return "-";


                default:
                    return @operator;
            }
        }
    }
}
