import React, {Component} from "react";
import PropTypes from "prop-types";
import { connect } from "react-redux";
import { GridSystem, GridCell, Switch, SingleLineInputWithError, Button, InputGroup }  from "@dnnsoftware/dnn-react-common";
import SettingsActions from "../../actions/settings";
import resx from "../../resources";
import "./generalSettings.less";
import utils from "../../utils";

class GeneralSettings extends Component {

    constructor() {
        super();

        this.state = {
            error: {
                appId: false,
                appSecret: false,
                tenantName: false,
                tenantId: false
            }
        };
    }
    UNSAFE_componentWillMount() {
        const {props} = this;

        props.dispatch(SettingsActions.getSettings());
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        const {state} = this;

        state.error["appId"] = (nextProps.apiKey === "");
        state.error["appSecret"] = (nextProps.apiSecret === "");
        state.error["tenantName"] = (nextProps.tenantName === "");
        state.error["tenantId"] = (nextProps.tenantId === "");
        state.error["signUpPolicy"] = (nextProps.signUpPolicy === "");
        state.error["profilePolicy"] = (nextProps.profilePolicy === "");
        state.error["passwordResetPolicy"] = (nextProps.passwordResetPolicy === "");
    }   

    onSettingChange(key, event) {
        let {props} = this;

        props.dispatch(SettingsActions.settingsClientModified({
            enabled: (key === "AADB2CProviderEnabled") ? !props.enabled : props.enabled,
            autoRedirect: (key === "AutoRedirect") ? !props.autoRedirect : props.autoRedirect,
            apiKey: (key === "AppId") ? event.target.value : props.apiKey,
            apiSecret: (key === "AppSecret") ? event.target.value : props.apiSecret,
            tenantName: (key === "TenantName") ? event.target.value : props.tenantName,
            tenantId: (key === "TenantId") ? event.target.value : props.tenantId,
            signUpPolicy: (key === "SignUpPolicy") ? event.target.value : props.signUpPolicy,
            profilePolicy: (key === "ProfilePolicy") ? event.target.value : props.profilePolicy,
            passwordResetPolicy: (key === "PasswordResetPolicy") ? event.target.value : props.passwordResetPolicy,
        }));
    }

    onClickCancel() {
        utils.utilities.closePersonaBar();
    }

    onClickSave() {
        event.preventDefault();
        let {props} = this;

        props.dispatch(SettingsActions.updateGeneralSettings({
            enabled: props.enabled,
            autoRedirect: props.autoRedirect,
            apiKey: props.apiKey,
            apiSecret: props.apiSecret,
            tenantName: props.tenantName,
            tenantId: props.tenantId,
            signUpPolicy: props.signUpPolicy,
            profilePolicy: props.profilePolicy,
            passwordResetPolicy: props.passwordResetPolicy,
        }, () => {
            utils.utilities.notify(resx.get("SettingsUpdateSuccess"));
            this.setState({
                clientModified: false
            });            
        }, () => {
            utils.utilities.notifyError(resx.get("SettingsError"));
        }));
    }

    render() {
        return (
            <div className="dnn-azuread-b2c-generalSettings">
                <GridCell columnSize={50}>
                    <p className="panel-description">{resx.get("lblTabDescription")}</p>
                    <Switch label={resx.get("lblEnabled")}
                        onText=""
                        offText=""
                        value={this.props.enabled}
                        tooltipMessage={resx.get("lblEnabled.Help")}
                        onChange={this.onSettingChange.bind(this, "AADB2CProviderEnabled")} />
                    <Switch label={resx.get("lblAutoRedirect")}
                        onText=""
                        offText=""
                        tooltipMessage={resx.get("lblAutoRedirect.Help")}
                        value={this.props.autoRedirect}
                        onChange={this.onSettingChange.bind(this, "AutoRedirect")} />
                </GridCell>
                <GridCell columnSize={50}>
                    <div className="logo"></div>
                </GridCell>
                <GridSystem className="with-right-border top-half">
                    <GridCell columnSize={90}>
                        <h1>{resx.get("lblDirectory")}</h1>
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblTenantName")}
                            enabled={true}
                            error={this.state.error.tenantName}
                            errorMessage={resx.get("lblTenantName.Error")}
                            tooltipMessage={resx.get("lblTenantName.Help")}
                            value={this.props.tenantName}
                            onChange={this.onSettingChange.bind(this, "TenantName")}
                        />                        
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblTenantId")}
                            enabled={true}
                            error={this.state.error.tenantId}
                            errorMessage={resx.get("lblTenantId.Error")}
                            tooltipMessage={resx.get("lblTenantId.Help")}
                            value={this.props.tenantId}
                            onChange={this.onSettingChange.bind(this, "TenantId")}
                        />
                        {/* <h1>{resx.get("lblProviderCredentials")}</h1> */}
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblAppId")}
                            enabled={true}
                            error={this.state.error.appId}
                            errorMessage={resx.get("lblAppId.Error")}
                            tooltipMessage={resx.get("lblAppId.Help")}
                            value={this.props.apiKey}
                            onChange={this.onSettingChange.bind(this, "AppId")}
                        />
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblAppSecret")}
                            type="password"
                            enabled={true}
                            error={this.state.error.appSecret}
                            errorMessage={resx.get("lblAppSecret.Error")}
                            tooltipMessage={resx.get("lblAppSecret.Help")}
                            value={this.props.apiSecret}
                            autoComplete="off"
                            onChange={this.onSettingChange.bind(this, "AppSecret")}
                        />
                    </GridCell>
                    <GridCell columnSize={100}>
                        <h1>{resx.get("lblPolicies")}</h1>
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblSignUpPolicyName")}
                            enabled={true}
                            error={this.state.error.signUpPolicy}
                            errorMessage={resx.get("lblSignUpPolicyName.Error")}
                            tooltipMessage={resx.get("lblSignUpPolicyName.Help")}
                            value={this.props.signUpPolicy}
                            onChange={this.onSettingChange.bind(this, "SignUpPolicy")}
                        />
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblProfilePolicyName")}
                            enabled={true}
                            error={this.state.error.profilePolicy}
                            errorMessage={resx.get("lblProfilePolicyName.Error")}
                            tooltipMessage={resx.get("lblProfilePolicyName.Help")}
                            value={this.props.profilePolicy}
                            onChange={this.onSettingChange.bind(this, "ProfilePolicy")}
                        />
                        <SingleLineInputWithError
                            withLabel={true}
                            label={resx.get("lblPasswordResetPolicyName")}
                            enabled={true}
                            error={this.state.error.passwordResetPolicy}
                            errorMessage={resx.get("lblPasswordResetPolicyName.Error")}
                            tooltipMessage={resx.get("lblPasswordResetPolicyName.Help")}
                            value={this.props.passwordResetPolicy}
                            onChange={this.onSettingChange.bind(this, "PasswordResetPolicy")}
                        />
                    </GridCell>
                </GridSystem>
                <InputGroup>
                    <div className="buttons-box">
                        <Button
                            disabled={false}
                            type="secondary"
                            onClick={this.onClickCancel.bind(this)}
                        >
                            {resx.get("Cancel")}
                        </Button>
                        <Button
                            disabled={this.state.error.appId || this.state.error.appSecret || this.state.error.tenantName || this.state.error.tenantId}
                            type="primary"
                            onClick={this.onClickSave.bind(this)}
                        >
                            {resx.get("SaveSettings")}
                        </Button>
                    </div>
                </InputGroup>
            </div>
        );
    }
}

GeneralSettings.propTypes = {
    dispatch: PropTypes.func.isRequired,
    enabled: PropTypes.bool,
    autoRedirect: PropTypes.bool,
    apiKey: PropTypes.string,
    apiSecret: PropTypes.string,
    tenantName: PropTypes.string,
    tenantId: PropTypes.string,
    signUpPolicy: PropTypes.string,
    profilePolicy: PropTypes.string,
    passwordResetPolicy: PropTypes.string
};


function mapStateToProps(state) {
    return {
        enabled: state.settings.enabled,
        autoRedirect: state.settings.autoRedirect,
        apiKey: state.settings.apiKey,
        apiSecret: state.settings.apiSecret,
        tenantName: state.settings.tenantName,
        tenantId: state.settings.tenantId,
        signUpPolicy: state.settings.signUpPolicy,
        profilePolicy: state.settings.profilePolicy,
        passwordResetPolicy: state.settings.passwordResetPolicy
    };
}

export default connect(mapStateToProps)(GeneralSettings);