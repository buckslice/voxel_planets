using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class that creates cache of all WaitForSeconds calls to heavily reduce allocations
// found here: http://blog.tochasstudios.com/2015/11/cache-coroutines.html
public static class Yielders {

    static Dictionary<float, WaitForSeconds> _timeInterval = new Dictionary<float, WaitForSeconds>(100);

    static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
    public static WaitForEndOfFrame EndOfFrame {
        get { return _endOfFrame; }
    }

    static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
    public static WaitForFixedUpdate FixedUpdate {
        get { return _fixedUpdate; }
    }

    public static WaitForSeconds Get(float seconds) {
        if (!_timeInterval.ContainsKey(seconds))
            _timeInterval.Add(seconds, new WaitForSeconds(seconds));
        return _timeInterval[seconds];
    }

}