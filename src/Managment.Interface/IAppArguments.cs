namespace Managment.Interface
{
    public interface IAppArguments
    {
        public bool Update { get; set; }

        public bool Install { get; set; }

        public bool Run { get; set; }

        /// <summary>
        /// The number of parameters will be checked, there should be no more than one
        /// </summary>
        /// <returns>True if validate, false otherwise</returns>
        public bool ValidateParameters();
    }
}
