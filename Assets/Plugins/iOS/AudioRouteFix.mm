#import <AVFoundation/AVFoundation.h>

extern "C" void _ios_audio_set_playback_category()
{
    NSError *error = nil;

    AVAudioSession *session = [AVAudioSession sharedInstance];

    BOOL ok = [session setCategory:AVAudioSessionCategoryPlayback
                       withOptions:AVAudioSessionCategoryOptionMixWithOthers
                             error:&error];

    if (!ok) {
        NSLog(@"[AudioRouteFix] setCategory failed: %@", error);
    }

    ok = [session setActive:YES error:&error];
    if (!ok) {
        NSLog(@"[AudioRouteFix] setActive failed: %@", error);
    }
}
