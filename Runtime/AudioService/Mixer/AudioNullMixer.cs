using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProvisGames.Core.AudioSystem;

namespace Assets.ProvisGames.AudioService.Mixer
{
    public class AudioNullMixer : Mixer<AudioTrack.AudioPlayer>
    {
        protected override void BeforeMixUpdate(float deltaTime)
        {}

        protected override void PrepareMix(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {}
        protected override bool Mixing(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            return false;
        }
        protected override void AfterMixUpdate(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {}
    }
}
