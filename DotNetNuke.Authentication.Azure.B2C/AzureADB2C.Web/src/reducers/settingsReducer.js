import { settings as ActionTypes } from "../constants/actionTypes";

export default function settings(state = {
    selectedTab: 0
}, action) {
    switch (action.type) {
        case ActionTypes.RETRIEVED_SETTINGS:
            return { ...state,
                enabled: action.data.enabled,
                autoRedirect: action.data.autoRedirect,
                apiKey: action.data.apiKey,
                apiSecret: action.data.apiSecret,
                tenantName: action.data.tenantName,
                tenantId: action.data.tenantId,
                signUpPolicy: action.data.signUpPolicy,
                profilePolicy: action.data.profilePolicy,
                passwordResetPolicy: action.data.passwordResetPolicy,
                aadAppClientId: action.data.aadAppClientId,
                aadAppSecret: action.data.aadAppSecret,
                jwtAudiences: action.data.jwtAudiences,
                roleSyncEnabled: action.data.roleSyncEnabled,
                profileSyncEnabled: action.data.profileSyncEnabled,
                jwtAuthEnabled: action.data.jwtAuthEnabled,
                apiResource: action.data.apiResource,
                scopes: action.data.scopes,
                clientModified: action.data.clientModified
            };
        case ActionTypes.SETTINGS_CLIENT_MODIFIED:
            return { ...state,
                enabled: action.data.enabled,
                autoRedirect: action.data.autoRedirect,
                apiKey: action.data.apiKey,
                apiSecret: action.data.apiSecret,
                tenantName: action.data.tenantName,
                tenantId: action.data.tenantId,
                signUpPolicy: action.data.signUpPolicy,
                profilePolicy: action.data.profilePolicy,
                passwordResetPolicy: action.data.passwordResetPolicy,
                aadAppClientId: action.data.aadAppClientId,
                aadAppSecret: action.data.aadAppSecret,
                jwtAudiences: action.data.jwtAudiences,
                roleSyncEnabled: action.data.roleSyncEnabled,
                profileSyncEnabled: action.data.profileSyncEnabled,
                jwtAuthEnabled: action.data.jwtAuthEnabled,
                apiResource: action.data.apiResource,
                scopes: action.data.scopes,
                clientModified: action.data.clientModified
            };
        case ActionTypes.UPDATED_SETTINGS:
            return { ...state,
                clientModified: action.data.clientModified
            };            
        case ActionTypes.SWITCH_TAB:
            return {
                ...state,
                selectedTab: action.payload
            };            
        default:
            return { ...state
            };
    }
}
