namespace SoundIO
{
    //
    // Enumerations defined in libsoundio
    //

    public enum Error
    {
        None,
        NoMem,
        InitAudioBackend,
        SystemResources,
        OpeningDevice,
        NoSuchDevice,
        Invalid,
        BackendUnavailable,
        Streaming,
        IncompatibleDevice,
        NoSuchClient,
        IncompatibleBackend,
        BackendDisconnected,
        Underflow,
        EncodingString
    }

    public enum Backend
    {
        None,
        Jack,
        PulseAudio,
        Alsa,
        CoreAudio,
        Wasapi,
        Dummy
    }

    public enum DeviceAim { Input, Output };

    public enum Format
    {
        Invalid,
        S8, U8,
        S16LE, S16BE, U16LE, U16BE,
        S24LE, S24BE, U24LE, U24BE,
        S32LE, S32BE, U32LE, U32BE,
        Float32LE, Float32BE, Float64LE, Float64BE,
    }

    public enum Channel
    {
        Invalid,

        FrontLeft, FrontRight, FrontCenter,
        Lfe,
        BackLeft, BackRight,
        FrontLeftCenter, FrontRightCenter,
        BackCenter,
        SideLeft, SideRight,
        TopCenter,
        TopFrontLeft, TopFrontCenter, TopFrontRight,
        TopBackLeft, TopBackCenter, TopBackRight,

        BackLeftCenter, BackRightCenter,
        FrontLeftWide, FrontRightWide,
        FrontLeftHigh, FrontCenterHigh, FrontRightHigh,
        TopFrontLeftCenter, TopFrontRightCenter,
        TopSideLeft, TopSideRight,
        LeftLfe, RightLfe, Lfe2,
        BottomCenter, BottomLeftCenter, BottomRightCenter,

        MsMid, MsSide,

        AmbisonicW, AmbisonicX, AmbisonicY, AmbisonicZ,

        XyX, XyY,

        HeadphonesLeft, HeadphonesRight,
        ClickTrack,
        ForeignLanguage,
        HearingImpaired,
        Narration,
        Haptic,
        DialogCentricMix,

        Aux, Aux0, Aux1, Aux2, Aux3, Aux4, Aux5, Aux6, Aux7,
        Aux8, Aux9, Aux10, Aux11, Aux12, Aux13, Aux14, Aux15
    }
}
