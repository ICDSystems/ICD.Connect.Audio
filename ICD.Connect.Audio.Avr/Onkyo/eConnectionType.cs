namespace ICD.Connect.Audio.Avr.Onkyo
{
    public enum eCommunicationsType
    {
        Auto, // Determine the connection type from the connected port
        Serial, // Force connection type to serial
        Ethernet // Force connection type to ethernet
    }
}