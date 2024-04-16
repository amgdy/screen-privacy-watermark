# Screen Privacy Watermark OSS Tool

## Overview
The Screen Privacy Watermark OSS Tool is a tool that adds a watermark to the screen to protect privacy. It allows you to customize the watermark text, font, color, opacity, and other formatting options. The tool supports different data sources such as Entra ID and local sources, and provides various policy configurations to control when and how the watermark is displayed. Additionally, it offers options for caching the watermark text and integrating with Application Insights for monitoring purposes.

## Configurations

**Source: EntraId**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| Source:EntraId:Enabled | true/false | true| Determines if the Entra ID source is enabled |
| Source:EntraId:Attributes | text | Id,DisplayName,UserPrincipalName,Mail,GivenName,Surname | Specifies the attributes to be used from the Entra ID |

**Source: Local**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| Source:Local:Enabled | true/false | true| Determines if the local source is enabled |
| Source:Local:DateCultures | text | | Specifies the date cultures to be used in the local source |

**Policy**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| Policy:EvaluationMode | Any/All | Any | Determines the evaluation mode for displaying the watermark |
| Policy:EntraIdGroups:AllowedGroupsIds | text | | Specifies the Entra ID groups that are allowed to display the watermark |
| Policy:MacAddress:AllowedMacAddresses | text | | Specifies the MAC addresses that are allowed to display the watermark |
| Policy:Network:AllowedIPs | text | | Specifies the IP addresses that are allowed to display the watermark |
| Policy:Network:AllowedCidrs | text | | Specifies the CIDRs that are allowed to display the watermark |
| Policy:Process:AllowedProcesses | text | | Specifies the processes that are allowed to display the watermark when they have any windows opened and not minimized |
| Policy:Process:EnableWildcardNames | true/false | false | Determines if wildcard names are enabled for process names |

**EntraID**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| EntraID:ClientId | text | | Specifies the client ID for Entra ID |
| EntraID:ClientSecret | text | | Specifies the client secret for Entra ID |
| EntraID:TenantId | text | | Specifies the tenant ID for Entra ID |
| EntraID:UsePublicClient | true/false | true | Determines if a public client is used for Entra ID authentication |

**Watermark and Format**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| Watermark:ConnectedPattern | text | {UserPrincipalName} | Specifies the watermark pattern for connected/online users |
| Watermark:DisconnectedPattern | text | {UserName}| Specifies the watermark pattern for disconnected/offline users |
| Watermark:EnableWatermarkTextCache | true/false | true | Determines if the watermark text cache is enabled when disconnected |
| Format:FontSize | number | 16 | Specifies the font size of the watermark text |
| Format:FontName | text | Segoe UI | Specifies the font name of the watermark text |
| Format:Opacity | number | 40 | Specifies the opacity of the watermark text where 100 is totally transparent and 0 is totally opaque |
| Format:Color | Color | Gray | Specifies the color of the watermark text |
| Format:OutlineColor | Color |  | Specifies the outline color of the watermark text. If not set, the outline will not be displayed |
| Format:OutlineWidth | number | 0.5 | Specifies the outline width of the watermark text if outline color is set |
| Format:UseDynamicsSpacing | true/false | false | Determines if dynamic spacing is used for the watermark text words |
| Format:LinesCount | number | 8 | Specifies the number of lines of the watermark text on the screen |

**Application Insights**

| Configuration | Type | Default | Description |
| ------------- | ---- | ------- | ----------- |
| ApplicationInsights:ConnectionString | text | | Specifies the connection string for Application Insights |


## Configuring the tool

    - Deploy Azure App Configuration and set the configurations
