//
//  UICKeyChainStoreWrapper.mm
//  Unity-iPhone
//
//  Created by Taewoo Han on 2022/12/02.
//
#import <Foundation/Foundation.h>
#import "UICKeyChainStore.h"

static UICKeyChainStore* keychain = NULL;

char* convertNSStringToCString(const NSString* nsString)
{
    if (nsString == NULL)
        return NULL;

    const char* nsStringUtf8 = [nsString UTF8String];
    //create a null terminated C string on the heap so that our string's memory isn't wiped out right after method's return
    char* cString = (char*)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);

    return cString;
}

UICKeyChainStore* getKeyChain() {
    if (keychain == NULL) {
        keychain = [UICKeyChainStore keyChainStoreWithService:[[NSBundle mainBundle] bundleIdentifier]];
    }
    return keychain;
}

extern "C" {
const char* getKeyChainValue(const char* key) {
    NSString *value = [getKeyChain() stringForKey:[NSString stringWithUTF8String:key]];
    return convertNSStringToCString(value);
}

void setKeyChainValue(const char* key, const char* value)
{
    [getKeyChain() setString:[NSString stringWithUTF8String:value]
                      forKey:[NSString stringWithUTF8String:key]];
}

void deleteKeyChainValue(const char* key)
{
    [getKeyChain() removeItemForKey:[NSString stringWithUTF8String:key]];
}
}
